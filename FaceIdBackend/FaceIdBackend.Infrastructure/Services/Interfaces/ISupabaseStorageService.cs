using Microsoft.AspNetCore.Http;

namespace FaceIdBackend.Infrastructure.Services.Interfaces;

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
}
