using FaceIdBackend.Application.Services.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FaceIdBackend.Application.Services;

/// <summary>
/// Service for scheduling Hangfire background jobs for attendance sessions
/// Manages preload and cleanup jobs for face database synchronization
/// </summary>
public class AttendanceSessionJobScheduler : IAttendanceSessionJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<AttendanceSessionJobScheduler> _logger;
    private static readonly Dictionary<Guid, List<string>> _sessionJobs = new();
    private static readonly object _lock = new();

    public AttendanceSessionJobScheduler(
        IBackgroundJobClient backgroundJobClient,
        ILogger<AttendanceSessionJobScheduler> _logger)
    {
        _backgroundJobClient = backgroundJobClient;
        this._logger = _logger;
    }

    /// <inheritdoc />
    public string SchedulePreloadJob(Guid sessionId, Guid classId, DateTime startTime, int preloadMinutesBefore = 10)
    {
        try
        {
            // Calculate preload time (in UTC)
            var preloadTime = startTime.AddMinutes(-preloadMinutesBefore);
            var now = DateTime.UtcNow;

            _logger.LogInformation(
                "📅 Scheduling preload job for Session {SessionId}, Class {ClassId}. Start: {StartTime}, Preload: {PreloadTime} ({Minutes} min before)",
                sessionId,
                classId,
                startTime.ToString("yyyy-MM-dd HH:mm:ss"),
                preloadTime.ToString("yyyy-MM-dd HH:mm:ss"),
                preloadMinutesBefore
            );

            // If preload time is in the past, execute immediately
            if (preloadTime <= now)
            {
                _logger.LogWarning(
                    "⚠️ Preload time is in the past! Scheduling immediate execution. Preload: {PreloadTime}, Now: {Now}",
                    preloadTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    now.ToString("yyyy-MM-dd HH:mm:ss")
                );
                preloadTime = now.AddSeconds(5); // Execute in 5 seconds
            }

            // Schedule the job
            var jobId = _backgroundJobClient.Schedule<IFaceDatabaseSyncService>(
                service => service.PreloadClassDatabaseAsync(classId),
                preloadTime
            );

            // Track job for this session
            lock (_lock)
            {
                if (!_sessionJobs.ContainsKey(sessionId))
                {
                    _sessionJobs[sessionId] = new List<string>();
                }
                _sessionJobs[sessionId].Add(jobId);
            }

            _logger.LogInformation(
                "✅ Preload job scheduled successfully. JobId: {JobId}, Session: {SessionId}",
                jobId,
                sessionId
            );

            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed to schedule preload job for Session {SessionId}, Class {ClassId}",
                sessionId,
                classId
            );
            throw;
        }
    }

    /// <inheritdoc />
    public string ScheduleCleanupJob(Guid sessionId, Guid classId, DateTime endTime, int cleanupMinutesAfter = 30)
    {
        try
        {
            // Calculate cleanup time (in UTC)
            var cleanupTime = endTime.AddMinutes(cleanupMinutesAfter);
            var now = DateTime.UtcNow;

            _logger.LogInformation(
                "📅 Scheduling cleanup job for Session {SessionId}, Class {ClassId}. End: {EndTime}, Cleanup: {CleanupTime} ({Minutes} min after)",
                sessionId,
                classId,
                endTime.ToString("yyyy-MM-dd HH:mm:ss"),
                cleanupTime.ToString("yyyy-MM-dd HH:mm:ss"),
                cleanupMinutesAfter
            );

            // If cleanup time is in the past, execute immediately
            if (cleanupTime <= now)
            {
                _logger.LogWarning(
                    "⚠️ Cleanup time is in the past! Scheduling immediate execution. Cleanup: {CleanupTime}, Now: {Now}",
                    cleanupTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    now.ToString("yyyy-MM-dd HH:mm:ss")
                );
                cleanupTime = now.AddSeconds(5); // Execute in 5 seconds
            }

            // Schedule the job
            var jobId = _backgroundJobClient.Schedule<IFaceDatabaseSyncService>(
                service => service.CleanupClassDatabaseAsync(classId),
                cleanupTime
            );

            // Track job for this session
            lock (_lock)
            {
                if (!_sessionJobs.ContainsKey(sessionId))
                {
                    _sessionJobs[sessionId] = new List<string>();
                }
                _sessionJobs[sessionId].Add(jobId);
            }

            _logger.LogInformation(
                "✅ Cleanup job scheduled successfully. JobId: {JobId}, Session: {SessionId}",
                jobId,
                sessionId
            );

            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed to schedule cleanup job for Session {SessionId}, Class {ClassId}",
                sessionId,
                classId
            );
            throw;
        }
    }

    /// <inheritdoc />
    public bool CancelJob(string jobId)
    {
        try
        {
            var result = _backgroundJobClient.Delete(jobId);

            if (result)
            {
                _logger.LogInformation("🚫 Cancelled job: {JobId}", jobId);
            }
            else
            {
                _logger.LogWarning("⚠️ Failed to cancel job (may not exist): {JobId}", jobId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error cancelling job: {JobId}", jobId);
            return false;
        }
    }

    /// <inheritdoc />
    public int CancelSessionJobs(Guid sessionId)
    {
        try
        {
            lock (_lock)
            {
                if (!_sessionJobs.ContainsKey(sessionId))
                {
                    _logger.LogInformation("No jobs found for Session {SessionId}", sessionId);
                    return 0;
                }

                var jobIds = _sessionJobs[sessionId];
                var cancelledCount = 0;

                foreach (var jobId in jobIds)
                {
                    if (CancelJob(jobId))
                    {
                        cancelledCount++;
                    }
                }

                _sessionJobs.Remove(sessionId);

                _logger.LogInformation(
                    "🚫 Cancelled {Count}/{Total} jobs for Session {SessionId}",
                    cancelledCount,
                    jobIds.Count,
                    sessionId
                );

                return cancelledCount;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error cancelling jobs for Session {SessionId}", sessionId);
            return 0;
        }
    }

    /// <inheritdoc />
    public string? GetJobStatus(string jobId)
    {
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var jobDetails = monitoringApi.JobDetails(jobId);

            if (jobDetails == null)
            {
                return null;
            }

            return jobDetails.History
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefault()
                ?.StateName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting job status for: {JobId}", jobId);
            return null;
        }
    }
}
