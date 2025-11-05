namespace FaceIdBackend.Application.Services.Interfaces;

/// <summary>
/// Service for synchronizing face database with Python Flask API
/// </summary>
public interface IFaceDatabaseSyncService
{
    /// <summary>
    /// Preload class face database from Supabase to Python server
    /// </summary>
    /// <param name="classId">Class ID to preload</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> PreloadClassDatabaseAsync(Guid classId);

    /// <summary>
    /// Cleanup class face database from Python server
    /// </summary>
    /// <param name="classId">Class ID to cleanup</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> CleanupClassDatabaseAsync(Guid classId);

    /// <summary>
    /// Check if Python Flask API is available
    /// </summary>
    /// <returns>True if API is healthy, false otherwise</returns>
    Task<bool> IsFlaskApiHealthyAsync();
}
