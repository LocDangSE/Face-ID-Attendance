using FaceIdBackend.Domain.Data;

namespace FaceIdBackend.Application.Services.Interfaces;

/// <summary>
/// Service interface for managing attendance session lifecycle with Supabase integration
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Create a new attendance session with Supabase folder structure
    /// </summary>
    Task<AttendanceSession> CreateSessionAsync(Guid classId, DateOnly sessionDate, string? location = null);

    /// <summary>
    /// Upload detected faces image to session folder in Supabase
    /// </summary>
    Task<string> UploadDetectedFacesAsync(Guid sessionId, byte[] imageData, string fileName);

    /// <summary>
    /// Save session results (attendance records) to Supabase as JSON
    /// </summary>
    Task<string> SaveSessionResultsAsync(Guid sessionId, object results);

    /// <summary>
    /// Complete session and finalize all uploads
    /// </summary>
    Task CompleteSessionAsync(Guid sessionId);

    /// <summary>
    /// Delete session and all associated Supabase files
    /// </summary>
    Task DeleteSessionAsync(Guid sessionId);

    /// <summary>
    /// Get session details including Supabase URLs
    /// </summary>
    Task<AttendanceSession> GetSessionWithUrlsAsync(Guid sessionId);
}
