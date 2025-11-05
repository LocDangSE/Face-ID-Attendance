using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Domain.Data;
using FaceIdBackend.Infrastructure.Services.Interfaces;
using FaceIdBackend.Infrastructure.UnitOfWork;
using FaceIdBackend.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace FaceIdBackend.Application.Services;

/// <summary>
/// Service for managing attendance session lifecycle with Supabase integration
/// </summary>
public class SessionService : ISessionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupabaseStorageService _supabaseStorage;
    private readonly IAttendanceSessionJobScheduler _jobScheduler;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        IUnitOfWork unitOfWork,
        ISupabaseStorageService supabaseStorage,
        IAttendanceSessionJobScheduler jobScheduler,
        ILogger<SessionService> logger)
    {
        _unitOfWork = unitOfWork;
        _supabaseStorage = supabaseStorage;
        _jobScheduler = jobScheduler;
        _logger = logger;
    }

    public async Task<AttendanceSession> CreateSessionAsync(Guid classId, DateOnly sessionDate, string? location = null)
    {
        _logger.LogInformation("Creating attendance session for class {ClassId} on {Date}", classId, sessionDate);

        // Verify class exists
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null)
            throw new KeyNotFoundException($"Class with ID {classId} not found");

        // Check for existing active session
        var existingSession = await _unitOfWork.AttendanceSessions
            .FirstOrDefaultAsync(s => s.ClassId == classId &&
                                     s.SessionDate == sessionDate &&
                                     s.Status == "InProgress");

        if (existingSession != null)
            throw new InvalidOperationException($"An active session already exists for this class on {sessionDate}");

        // Create session
        var session = new AttendanceSession
        {
            SessionId = Guid.NewGuid(),
            ClassId = classId,
            SessionDate = sessionDate,
            SessionStartTime = TimezoneHelper.GetUtcNowForStorage(),
            Status = "InProgress",
            Location = location,
            CreatedAt = TimezoneHelper.GetUtcNowForStorage()
        };

        await _unitOfWork.AttendanceSessions.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        // Create Supabase folder structure if enabled
        if (_supabaseStorage.IsEnabled())
        {
            try
            {
                var folderPath = await _supabaseStorage.CreateSessionFolderAsync(session.SessionId, sessionDate);
                _logger.LogInformation("Created Supabase folder structure for session {SessionId}: {Path}",
                    session.SessionId, folderPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create Supabase folder structure for session {SessionId}",
                    session.SessionId);
            }
        }

        // Schedule automatic database preload and cleanup jobs
        try
        {
            // Calculate session end time (default to 2 hours after start if not provided)
            var sessionEndTime = session.SessionEndTime ?? session.SessionStartTime.AddHours(2);

            // Schedule preload job (10 minutes before session start)
            var preloadJobId = _jobScheduler.SchedulePreloadJob(
                session.SessionId,
                classId,
                session.SessionStartTime,
                preloadMinutesBefore: 10
            );

            // Schedule cleanup job (30 minutes after session end)
            var cleanupJobId = _jobScheduler.ScheduleCleanupJob(
                session.SessionId,
                classId,
                sessionEndTime,
                cleanupMinutesAfter: 30
            );

            // Store job IDs in session notes for tracking
            var jobInfo = new
            {
                PreloadJobId = preloadJobId,
                CleanupJobId = cleanupJobId,
                PreloadTime = session.SessionStartTime.AddMinutes(-10),
                CleanupTime = sessionEndTime.AddMinutes(30)
            };

            session.Notes = JsonSerializer.Serialize(jobInfo);
            _unitOfWork.AttendanceSessions.Update(session);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "✅ Scheduled background jobs for session {SessionId}: Preload={PreloadJobId}, Cleanup={CleanupJobId}",
                session.SessionId, preloadJobId, cleanupJobId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to schedule background jobs for session {SessionId}", session.SessionId);
            // Don't throw - session creation succeeded, job scheduling is optional
        }

        _logger.LogInformation("Successfully created session {SessionId}", session.SessionId);
        return session;
    }

    public async Task<string> UploadDetectedFacesAsync(Guid sessionId, byte[] imageData, string fileName)
    {
        _logger.LogInformation("Uploading detected faces image for session {SessionId}", sessionId);

        var session = await _unitOfWork.AttendanceSessions.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {sessionId} not found");

        if (!_supabaseStorage.IsEnabled())
        {
            _logger.LogWarning("Supabase storage is disabled, skipping upload");
            return string.Empty;
        }

        try
        {
            var publicUrl = await _supabaseStorage.UploadToSessionAsync(
                sessionId,
                session.SessionDate,
                imageData,
                fileName,
                "detected_faces",
                "image/jpeg");

            _logger.LogInformation("Successfully uploaded detected faces image: {Url}", publicUrl);
            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload detected faces image for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<string> SaveSessionResultsAsync(Guid sessionId, object results)
    {
        _logger.LogInformation("Saving session results for session {SessionId}", sessionId);

        var session = await _unitOfWork.AttendanceSessions.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {sessionId} not found");

        if (!_supabaseStorage.IsEnabled())
        {
            _logger.LogWarning("Supabase storage is disabled, skipping results save");
            return string.Empty;
        }

        try
        {
            var jsonContent = JsonSerializer.Serialize(results, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
            var fileName = $"results_{TimezoneHelper.GetUtcNowForStorage():yyyyMMdd_HHmmss}.json";

            var publicUrl = await _supabaseStorage.UploadToSessionAsync(
                sessionId,
                session.SessionDate,
                jsonBytes,
                fileName,
                "results",
                "application/json");

            _logger.LogInformation("Successfully saved session results: {Url}", publicUrl);
            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session results for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task CompleteSessionAsync(Guid sessionId)
    {
        _logger.LogInformation("Completing session {SessionId}", sessionId);

        var session = await _unitOfWork.AttendanceSessions
            .GetQueryable()
            .Include(s => s.Class)
            .Include(s => s.AttendanceRecords)
                .ThenInclude(r => r.Student)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            throw new KeyNotFoundException($"Session with ID {sessionId} not found");

        if (session.Status != "InProgress")
            throw new InvalidOperationException("Session is not in progress");

        session.SessionEndTime = TimezoneHelper.GetUtcNowForStorage();
        session.Status = "Completed";

        // Generate Session Snapshot for historical record
        try
        {
            var snapshot = await GenerateSessionSnapshotAsync(session);
            await _unitOfWork.SessionSnapshots.AddAsync(snapshot);

            _logger.LogInformation(
                "✅ Generated session snapshot {SnapshotId} for session {SessionId}. Present: {Present}/{Total}",
                snapshot.SnapshotId, sessionId, snapshot.PresentCount, snapshot.TotalStudents
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to generate session snapshot for session {SessionId}", sessionId);
            // Don't throw - session completion should succeed even if snapshot fails
        }

        _unitOfWork.AttendanceSessions.Update(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully completed session {SessionId}", sessionId);
    }

    private async Task<SessionSnapshot> GenerateSessionSnapshotAsync(AttendanceSession session)
    {
        _logger.LogInformation("Generating snapshot for session {SessionId}", session.SessionId);

        var records = session.AttendanceRecords.ToList();
        var totalEnrolled = session.Class.ClassEnrollments.Count(e => e.Status == "Active");

        var presentCount = records.Count(r => r.Status == "Present");
        var absentCount = totalEnrolled - presentCount;
        var lateCount = records.Count(r => r.Status == "Late");

        var attendanceRate = totalEnrolled > 0 ? (decimal)presentCount / totalEnrolled * 100 : 0;

        var sessionDuration = session.SessionEndTime.HasValue
            ? session.SessionEndTime.Value - session.SessionStartTime
            : TimeSpan.Zero;

        // Serialize attendance records
        var recordsData = records.Select(r => new
        {
            StudentId = r.StudentId,
            StudentNumber = r.Student.StudentNumber,
            StudentName = $"{r.Student.FirstName} {r.Student.LastName}",
            Status = r.Status,
            CheckInTime = r.CheckInTime,
            ConfidenceScore = r.ConfidenceScore,
            IsManualOverride = r.IsManualOverride,
            Notes = r.Notes
        }).ToList();

        var attendanceRecordsJson = JsonSerializer.Serialize(recordsData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Serialize session metadata
        var metadata = new
        {
            ClassName = session.Class.ClassName,
            ClassCode = session.Class.ClassCode,
            SessionDate = session.SessionDate,
            Location = session.Location,
            TotalEnrolled = totalEnrolled
        };

        var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Get captured images folder path from Supabase
        string? capturedImagesFolder = null;
        if (_supabaseStorage.IsEnabled())
        {
            try
            {
                capturedImagesFolder = $"sessions/{session.SessionDate:yyyy-MM-dd}/{session.SessionId}/captured";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not determine captured images folder path");
            }
        }

        var snapshot = new SessionSnapshot
        {
            SnapshotId = Guid.NewGuid(),
            SessionId = session.SessionId,
            TotalStudents = totalEnrolled,
            PresentCount = presentCount,
            AbsentCount = absentCount,
            LateCount = lateCount,
            AttendanceRate = Math.Round(attendanceRate, 2),
            CapturedImagesFolder = capturedImagesFolder,
            AttendanceRecordsJson = attendanceRecordsJson,
            SessionMetadataJson = metadataJson,
            GeneratedAt = TimezoneHelper.GetUtcNowForStorage(),
            SessionStartTime = session.SessionStartTime,
            SessionEndTime = session.SessionEndTime,
            SessionDuration = sessionDuration
        };

        // Upload snapshot to Supabase as JSON
        if (_supabaseStorage.IsEnabled())
        {
            try
            {
                var snapshotJson = JsonSerializer.Serialize(new
                {
                    snapshot.SnapshotId,
                    snapshot.SessionId,
                    snapshot.TotalStudents,
                    snapshot.PresentCount,
                    snapshot.AbsentCount,
                    snapshot.LateCount,
                    snapshot.AttendanceRate,
                    snapshot.SessionStartTime,
                    snapshot.SessionEndTime,
                    snapshot.SessionDuration,
                    AttendanceRecords = recordsData,
                    Metadata = metadata,
                    GeneratedAt = snapshot.GeneratedAt
                }, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var jsonBytes = Encoding.UTF8.GetBytes(snapshotJson);
                var fileName = $"snapshot_{session.SessionId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";

                await _supabaseStorage.UploadToSessionAsync(
                    session.SessionId,
                    session.SessionDate,
                    jsonBytes,
                    fileName,
                    "snapshots",
                    "application/json"
                );

                _logger.LogInformation("Uploaded snapshot JSON to Supabase: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upload snapshot to Supabase");
            }
        }

        return snapshot;
    }

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        _logger.LogInformation("Deleting session {SessionId}", sessionId);

        var session = await _unitOfWork.AttendanceSessions.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {sessionId} not found");

        // Cancel scheduled Hangfire jobs
        try
        {
            var cancelledCount = _jobScheduler.CancelSessionJobs(sessionId);
            _logger.LogInformation("Cancelled {Count} scheduled job(s) for session {SessionId}", cancelledCount, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cancel scheduled jobs for session {SessionId}", sessionId);
        }

        // Delete from Supabase if enabled
        if (_supabaseStorage.IsEnabled())
        {
            try
            {
                await _supabaseStorage.DeleteSessionFolderAsync(session.SessionId, session.SessionDate);
                _logger.LogInformation("Deleted Supabase folder for session {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete Supabase folder for session {SessionId}", sessionId);
            }
        }

        // Delete attendance records
        var records = await _unitOfWork.AttendanceRecords
            .GetQueryable()
            .Where(r => r.SessionId == sessionId)
            .ToListAsync();

        foreach (var record in records)
        {
            _unitOfWork.AttendanceRecords.Remove(record);
        }

        // Delete session
        _unitOfWork.AttendanceSessions.Remove(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted session {SessionId} and {Count} attendance records",
            sessionId, records.Count);
    }

    public async Task<AttendanceSession> GetSessionWithUrlsAsync(Guid sessionId)
    {
        var session = await _unitOfWork.AttendanceSessions
            .GetQueryable()
            .Include(s => s.Class)
            .Include(s => s.AttendanceRecords)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            throw new KeyNotFoundException($"Session with ID {sessionId} not found");

        return session;
    }
}
