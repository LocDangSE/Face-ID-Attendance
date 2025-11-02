using FaceIdBackend.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FaceIdBackend.Infrastructure.Services;

/// <summary>
/// Hybrid file storage service that uses Supabase cloud storage when enabled,
/// with local file storage as fallback
/// </summary>
public class HybridFileStorageService : IFileStorageService
{
    private readonly ISupabaseStorageService _supabaseStorage;
    private readonly FileStorageService _localStorage;
    private readonly ILogger<HybridFileStorageService> _logger;

    public HybridFileStorageService(
        ISupabaseStorageService supabaseStorage,
        FileStorageService localStorage,
        ILogger<HybridFileStorageService> logger)
    {
        _supabaseStorage = supabaseStorage;
        _localStorage = localStorage;
        _logger = logger;
    }

    public async Task<string> SaveStudentPhotoAsync(IFormFile file, Guid studentId)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{studentId}{extension}";

        // Check if Supabase is enabled
        if (!_supabaseStorage.IsEnabled())
        {
            var errorMsg = "❌ Supabase storage is not enabled. Please configure Supabase credentials in appsettings.json";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        // Upload to Supabase (no fallback)
        try
        {
            _logger.LogInformation("📤 Uploading student photo {StudentId} to Supabase cloud storage...", studentId);
            var publicUrl = await _supabaseStorage.UploadFileAsync(file, fileName, "students");
            _logger.LogInformation("✅ Successfully uploaded to Supabase: {Url}", publicUrl);
            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ FAILED to upload to Supabase for student {StudentId}. Error: {Message}", studentId, ex.Message);
            throw new Exception($"Failed to upload student photo to Supabase cloud storage: {ex.Message}", ex);
        }
    }

    public async Task DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        // If it's a Supabase URL, delete from Supabase
        if (_supabaseStorage.IsEnabled() &&
            (filePath.StartsWith("http://") || filePath.StartsWith("https://")))
        {
            try
            {
                _logger.LogInformation("Deleting file from Supabase: {Path}", filePath);
                await _supabaseStorage.DeleteFileAsync(filePath);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete from Supabase: {Path}", filePath);
            }
        }

        // Delete from local storage
        await _localStorage.DeleteFileAsync(filePath);
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        // For Supabase URLs, we assume they exist (checking would require additional API call)
        if (_supabaseStorage.IsEnabled() &&
            (filePath.StartsWith("http://") || filePath.StartsWith("https://")))
        {
            return Task.FromResult(true);
        }

        return _localStorage.FileExistsAsync(filePath);
    }

    public Stream GetFileStream(string filePath)
    {
        // This method is primarily for local files
        // For Supabase, the frontend will fetch directly from the public URL
        if (filePath.StartsWith("http://") || filePath.StartsWith("https://"))
        {
            throw new InvalidOperationException("Cannot get stream for remote Supabase files. Use the public URL directly.");
        }

        return _localStorage.GetFileStream(filePath);
    }
}
