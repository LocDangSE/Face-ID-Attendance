using Microsoft.AspNetCore.Http;

namespace FaceIdBackend.Infrastructure.Services.Interfaces;

/// <summary>
/// Service interface for Supabase storage operations with session-based folder management
/// </summary>
public interface ISupabaseStorageService
{
    /// <summary>
    /// Upload a file to Supabase Storage
    /// </summary>
    Task<string> UploadFileAsync(IFormFile file, string fileName, string folder = "");

    /// <summary>
    /// Upload file from byte array to Supabase Storage
    /// </summary>
    Task<string> UploadFileAsync(byte[] fileData, string fileName, string folder = "", string contentType = "image/jpeg");

    /// <summary>
    /// Delete a file from Supabase Storage
    /// </summary>
    Task<bool> DeleteFileAsync(string filePath);

    /// <summary>
    /// Get public URL for a file
    /// </summary>
    string GetPublicUrl(string filePath);

    /// <summary>
    /// Check if Supabase is properly configured and enabled
    /// </summary>
    bool IsEnabled();

    // ===== SESSION-BASED METHODS =====

    /// <summary>
    /// Create session folder structure in Supabase
    /// Creates: sessions/session_{id}_{date}/detected_faces/, embeddings/, results/
    /// </summary>
    /// <param name="sessionId">Unique session identifier</param>
    /// <param name="sessionDate">Session date for folder naming</param>
    /// <returns>Base session folder path</returns>
    Task<string> CreateSessionFolderAsync(Guid sessionId, DateOnly sessionDate);

    /// <summary>
    /// Upload file to specific session subfolder
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="sessionDate">Session date</param>
    /// <param name="fileData">File content as byte array</param>
    /// <param name="fileName">File name</param>
    /// <param name="subfolder">Subfolder name (detected_faces, embeddings, results)</param>
    /// <param name="contentType">MIME content type</param>
    /// <returns>Public URL of uploaded file</returns>
    Task<string> UploadToSessionAsync(
        Guid sessionId,
        DateOnly sessionDate,
        byte[] fileData,
        string fileName,
        string subfolder,
        string contentType = "image/jpeg");

    /// <summary>
    /// Download file from session folder
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="sessionDate">Session date</param>
    /// <param name="fileName">File name</param>
    /// <param name="subfolder">Subfolder name</param>
    /// <returns>File content as byte array</returns>
    Task<byte[]> DownloadFromSessionAsync(
        Guid sessionId,
        DateOnly sessionDate,
        string fileName,
        string subfolder);

    /// <summary>
    /// List all files in a session folder
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="sessionDate">Session date</param>
    /// <param name="subfolder">Optional subfolder name</param>
    /// <returns>List of file paths</returns>
    Task<List<string>> GetSessionFilesAsync(
        Guid sessionId,
        DateOnly sessionDate,
        string? subfolder = null);

    /// <summary>
    /// Delete entire session folder
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="sessionDate">Session date</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteSessionFolderAsync(Guid sessionId, DateOnly sessionDate);
}
