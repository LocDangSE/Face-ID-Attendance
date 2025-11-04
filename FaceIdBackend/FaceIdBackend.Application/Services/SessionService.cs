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
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        IUnitOfWork unitOfWork,
        ISupabaseStorageService supabaseStorage,
        ILogger<SessionService> logger)
    {
        _unitOfWork = unitOfWork;
        _supabaseStorage = supabaseStorage;
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

        var session = await _unitOfWork.AttendanceSessions.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {sessionId} not found");

        if (session.Status != "InProgress")
            throw new InvalidOperationException("Session is not in progress");

        session.SessionEndTime = TimezoneHelper.GetUtcNowForStorage();
        session.Status = "Completed";

        _unitOfWork.AttendanceSessions.Update(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully completed session {SessionId}", sessionId);
    }

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        _logger.LogInformation("Deleting session {SessionId}", sessionId);

        var session = await _unitOfWork.AttendanceSessions.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {sessionId} not found");

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
