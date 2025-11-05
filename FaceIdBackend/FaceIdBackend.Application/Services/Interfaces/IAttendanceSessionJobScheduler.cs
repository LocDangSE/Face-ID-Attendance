namespace FaceIdBackend.Application.Services.Interfaces;

/// <summary>
/// Service for scheduling Hangfire jobs for attendance sessions
/// </summary>
public interface IAttendanceSessionJobScheduler
{
    /// <summary>
    /// Schedule preload job to run before session starts
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="classId">Class ID to preload</param>
    /// <param name="startTime">Session start time</param>
    /// <param name="preloadMinutesBefore">Minutes before start time to preload (default: 10)</param>
    /// <returns>Hangfire job ID</returns>
    string SchedulePreloadJob(Guid sessionId, Guid classId, DateTime startTime, int preloadMinutesBefore = 10);

    /// <summary>
    /// Schedule cleanup job to run after session ends
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="classId">Class ID to cleanup</param>
    /// <param name="endTime">Session end time</param>
    /// <param name="cleanupMinutesAfter">Minutes after end time to cleanup (default: 30)</param>
    /// <returns>Hangfire job ID</returns>
    string ScheduleCleanupJob(Guid sessionId, Guid classId, DateTime endTime, int cleanupMinutesAfter = 30);

    /// <summary>
    /// Cancel a scheduled job
    /// </summary>
    /// <param name="jobId">Hangfire job ID</param>
    /// <returns>True if job was cancelled, false otherwise</returns>
    bool CancelJob(string jobId);

    /// <summary>
    /// Cancel all jobs associated with a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Number of jobs cancelled</returns>
    int CancelSessionJobs(Guid sessionId);

    /// <summary>
    /// Get job status
    /// </summary>
    /// <param name="jobId">Hangfire job ID</param>
    /// <returns>Job state name or null if not found</returns>
    string? GetJobStatus(string jobId);
}
