using FaceIdBackend.Infrastructure.Configuration;
using FaceIdBackend.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Supabase;

namespace FaceIdBackend.Infrastructure.Services;

public class SupabaseStorageService : ISupabaseStorageService
{
    private readonly SupabaseSettings _settings;
    private readonly ILogger<SupabaseStorageService> _logger;
    private readonly Lazy<Task<Client>> _supabaseClient;

    public SupabaseStorageService(
        IOptions<SupabaseSettings> settings,
        ILogger<SupabaseStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _supabaseClient = new Lazy<Task<Client>>(async () =>
        {
            if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.Key))
            {
                throw new InvalidOperationException("Supabase is not properly configured or is disabled");
            }

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };

            var client = new Client(_settings.Url, _settings.Key, options);
            await client.InitializeAsync();
            return client;
        });
    }

    public bool IsEnabled()
    {
        return _settings.Enabled &&
               !string.IsNullOrWhiteSpace(_settings.Url) &&
               !string.IsNullOrWhiteSpace(_settings.Key);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string fileName, string folder = "")
    {
        if (!IsEnabled())
        {
            var errorMsg = "❌ Supabase storage is not enabled. Please check appsettings.json: Supabase.Enabled should be true and credentials should be valid.";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileData = memoryStream.ToArray();

            return await UploadFileAsync(fileData, fileName, folder, file.ContentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error uploading file {FileName} to Supabase bucket {Bucket}", fileName, _settings.StorageBucket);
            throw;
        }
    }

    public async Task<string> UploadFileAsync(byte[] fileData, string fileName, string folder = "", string contentType = "image/jpeg")
    {
        if (!IsEnabled())
        {
            var errorMsg = "❌ Supabase storage is not enabled. Please check appsettings.json configuration.";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        try
        {
            _logger.LogInformation("📤 Connecting to Supabase bucket '{Bucket}'...", _settings.StorageBucket);
            var client = await _supabaseClient.Value;
            var storagePath = string.IsNullOrWhiteSpace(folder) ? fileName : $"{folder}/{fileName}";

            // Try to delete existing file first (Supabase doesn't auto-overwrite)
            try
            {
                await client.Storage
                    .From(_settings.StorageBucket)
                    .Remove(new List<string> { storagePath });
                _logger.LogInformation("🗑️ Removed existing file: {Path}", storagePath);
            }
            catch
            {
                // File doesn't exist, ignore error
            }

            // Upload new file
            _logger.LogInformation("⬆️ Uploading file to Supabase: {Path}", storagePath);
            await client.Storage
                .From(_settings.StorageBucket)
                .Upload(fileData, storagePath, new Supabase.Storage.FileOptions
                {
                    ContentType = contentType,
                    Upsert = true
                });

            var publicUrl = GetPublicUrl(storagePath);
            _logger.LogInformation("✅ Successfully uploaded file to Supabase: {Path}", storagePath);

            return publicUrl;
        }
        catch (Exception ex)
        {
            var errorMsg = $"❌ Failed to upload file to Supabase bucket '{_settings.StorageBucket}': {ex.Message}";
            _logger.LogError(ex, errorMsg);

            // Check for common issues
            if (ex.Message.Contains("404") || ex.Message.Contains("not found"))
            {
                errorMsg += " (Bucket may not exist - please create it in Supabase dashboard)";
            }
            else if (ex.Message.Contains("401") || ex.Message.Contains("unauthorized"))
            {
                errorMsg += " (Invalid credentials - check your Supabase Key)";
            }

            throw new Exception(errorMsg, ex);
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("Supabase storage is not enabled, skipping delete");
            return false;
        }

        try
        {
            // Extract path from public URL if needed
            var storagePath = ExtractStoragePath(filePath);

            var client = await _supabaseClient.Value;
            await client.Storage
                .From(_settings.StorageBucket)
                .Remove(new List<string> { storagePath });

            _logger.LogInformation("Successfully deleted file from Supabase: {Path}", storagePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FilePath} from Supabase", filePath);
            return false;
        }
    }

    public string GetPublicUrl(string filePath)
    {
        if (!IsEnabled())
        {
            return string.Empty;
        }

        // If it's already a full URL, return as-is
        if (filePath.StartsWith("http://") || filePath.StartsWith("https://"))
        {
            return filePath;
        }

        return string.Format(_settings.PublicUrlFormat, _settings.Url, _settings.StorageBucket, filePath);
    }

    private string ExtractStoragePath(string urlOrPath)
    {
        // If it's a full Supabase URL, extract the path
        if (urlOrPath.Contains("/storage/v1/object/public/"))
        {
            var parts = urlOrPath.Split(new[] { "/storage/v1/object/public/" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                var pathWithBucket = parts[1];
                var pathParts = pathWithBucket.Split('/', 2);
                return pathParts.Length > 1 ? pathParts[1] : pathWithBucket;
            }
        }

        return urlOrPath;
    }
}
