namespace FaceIdBackend.Application.Services.Interfaces;

/// <summary>
/// Service for synchronizing Python face recognition cache with database changes
/// Prevents cache drift by triggering Python cache updates after Student CRUD operations
/// </summary>
public interface ICacheSyncService
{
    /// <summary>
    /// Clear cache for a specific student (called after student deletion)
    /// </summary>
    /// <param name="studentId">Student ID to remove from cache</param>
    /// <returns>True if successful</returns>
    Task<bool> ClearStudentCacheAsync(Guid studentId);

    /// <summary>
    /// Re-register student face in cache (called after student update/photo change)
    /// </summary>
    /// <param name="studentId">Student ID to update in cache</param>
    /// <param name="photoUrl">URL or path to student photo</param>
    /// <returns>True if successful</returns>
    Task<bool> RefreshStudentCacheAsync(Guid studentId, string photoUrl);

    /// <summary>
    /// Clear all cache and force full reload (called after bulk operations)
    /// </summary>
    /// <returns>True if successful</returns>
    Task<bool> ClearAllCacheAsync();

    /// <summary>
    /// Enqueue background job to clear student cache (non-blocking)
    /// </summary>
    /// <param name="studentId">Student ID to remove from cache</param>
    /// <returns>Job ID</returns>
    string EnqueueClearStudentCache(Guid studentId);

    /// <summary>
    /// Enqueue background job to refresh student cache (non-blocking)
    /// </summary>
    /// <param name="studentId">Student ID to update</param>
    /// <param name="photoUrl">URL or path to photo</param>
    /// <returns>Job ID</returns>
    string EnqueueRefreshStudentCache(Guid studentId, string photoUrl);
}
