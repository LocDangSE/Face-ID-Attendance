using FaceIdBackend.Infrastructure.Configuration;
using FaceIdBackend.Infrastructure.Services.Interfaces;
using FaceIdBackend.Shared.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Supabase;

namespace FaceIdBackend.Infrastructure.Services;

/// <summary>
/// Enhanced Supabase Storage Service with session-based folder management and retry logic
/// </summary>
public class SupabaseStorageServiceEnhanced : ISupabaseStorageService
{
    private readonly SupabaseSettings _settings;
    private readonly ILogger<SupabaseStorageServiceEnhanced> _logger;
    private readonly Lazy<Task<Client>> _supabaseClient;
    private readonly AsyncRetryPolicy _retryPolicy;

    public SupabaseStorageServiceEnhanced(
        IOptions<SupabaseSettings> settings,
        ILogger<SupabaseStorageServiceEnhanced> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _supabaseClient = new Lazy<Task<Client>>(async () =>
        {
            if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.Url) ||
                string.IsNullOrWhiteSpace(_settings.ServiceKey))
            {
                throw new InvalidOperationException("Supabase is not properly configured or is disabled");
            }

            // Trim whitespace from URL and ServiceKey to avoid JWT parsing issues
            var url = _settings.Url.Trim();
            var serviceKey = _settings.ServiceKey.Trim();

            _logger.LogDebug("🔐 Initializing Supabase client with URL: {Url}", url);
            _logger.LogDebug("🔐 Service key length: {Length} characters", serviceKey.Length);

            // Initialize with custom headers for service_role authentication
            var options = new SupabaseOptions
            {
                AutoRefreshToken = false,
                AutoConnectRealtime = false,
                Headers = new Dictionary<string, string>
                {
                    { "apikey", serviceKey }  // Supabase requires this header
                }
            };

            var client = new Client(url, serviceKey, options);
            await client.InitializeAsync();

            _logger.LogInformation("✅ Supabase client initialized with service role key");
            return client;
        });

        // Configure Polly retry policy
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: _settings.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(_settings.MaxRetries * retryAttempt),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount}/{MaxRetries} for {Operation} after {Delay}ms. Error: {Error}",
                        retryCount, _settings.MaxRetries, context.OperationKey,
                        timeSpan.TotalMilliseconds, exception.Message);
                });
    }

    public bool IsEnabled()
    {
        return _settings.Enabled &&
               !string.IsNullOrWhiteSpace(_settings.Url) &&
               !string.IsNullOrWhiteSpace(_settings.ServiceKey);
    }

    /// <summary>
    /// Determines which bucket to use based on folder path
    /// </summary>
    private string GetBucketForFolder(string folder)
    {
        // Student photos go to student-photos bucket
        if (folder == "students")
        {
            return _settings.StudentPhotosBucket;
        }
        // Session-related data goes to attendance-sessions bucket
        return _settings.AttendanceSessionsBucket;
    }

    #region Legacy Methods (Backward Compatibility)

    public async Task<string> UploadFileAsync(IFormFile file, string fileName, string folder = "")
    {
        if (!IsEnabled())
        {
            var errorMsg = "❌ Supabase storage is not enabled. Please check appsettings.json configuration.";
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
            _logger.LogError(ex, "❌ Error uploading file {FileName} to Supabase", fileName);
            throw;
        }
    }

    public async Task<string> UploadFileAsync(
        byte[] fileData,
        string fileName,
        string folder = "",
        string contentType = "image/jpeg")
    {
        if (!IsEnabled())
        {
            var errorMsg = "❌ Supabase storage is not enabled.";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        return await ExecuteWithRetryAsync(async () =>
        {
            _logger.LogInformation("📤 Uploading file to Supabase: {FileName}", fileName);
            var client = await _supabaseClient.Value;
            var storagePath = string.IsNullOrWhiteSpace(folder) ? fileName : $"{folder}/{fileName}";

            // Upload new file - determine bucket based on folder
            var bucket = GetBucketForFolder(folder);

            // Try to delete existing file first
            try
            {
                await client.Storage
                    .From(bucket)
                    .Remove(new List<string> { storagePath });
                _logger.LogDebug("🗑️ Removed existing file: {Path}", storagePath);
            }
            catch
            {
                // File doesn't exist, ignore
            }

            // Upload new file
            await client.Storage
                .From(bucket)
                .Upload(fileData, storagePath, new Supabase.Storage.FileOptions
                {
                    ContentType = contentType,
                    Upsert = true
                });

            var publicUrl = GetPublicUrl(storagePath, bucket);
            _logger.LogInformation("✅ Successfully uploaded file to {Bucket}: {Path}", bucket, storagePath);

            return publicUrl;
        }, "UploadFile");
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
            return await ExecuteWithRetryAsync(async () =>
            {
                var storagePath = ExtractStoragePath(filePath);
                var client = await _supabaseClient.Value;

                // Determine bucket - try attendance sessions first, then student photos
                var bucket = _settings.AttendanceSessionsBucket;
                try
                {
                    await client.Storage
                        .From(bucket)
                        .Remove(new List<string> { storagePath });
                }
                catch
                {
                    // Try student photos bucket
                    bucket = _settings.StudentPhotosBucket;
                    await client.Storage
                        .From(bucket)
                        .Remove(new List<string> { storagePath });
                }

                _logger.LogInformation("🗑️ Successfully deleted file from {Bucket}: {Path}", bucket, storagePath);
                return true;
            }, "DeleteFile");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting file {FilePath}", filePath);
            return false;
        }
    }

    public string GetPublicUrl(string filePath)
    {
        // Default to attendance sessions bucket for backward compatibility
        return GetPublicUrl(filePath, _settings.AttendanceSessionsBucket);
    }

    private string GetPublicUrl(string filePath, string bucket)
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

        return string.Format(_settings.PublicUrlFormat, _settings.Url, bucket, filePath);
    }

    #endregion

    #region Session-Based Methods

    public async Task<string> CreateSessionFolderAsync(Guid sessionId, DateOnly sessionDate)
    {
        if (!IsEnabled())
        {
            throw new InvalidOperationException("Supabase storage is not enabled");
        }

        return await ExecuteWithRetryAsync(async () =>
        {
            var sessionFolder = GetSessionFolderPath(sessionId, sessionDate);
            _logger.LogInformation("📁 Creating session folder structure: {Folder}", sessionFolder);

            // Create placeholder files in subfolders to ensure they exist
            var subfolders = new[] { "detected_faces", "embeddings", "results" };

            foreach (var subfolder in subfolders)
            {
                var placeholderPath = $"{sessionFolder}/{subfolder}/.keep";
                // Make placeholder file larger - some storage systems reject very small files
                var placeholderContent = $"# Session folder placeholder\n# Created: {TimezoneHelper.GetUtcNowForStorage():yyyy-MM-dd HH:mm:ss} UTC\n# Path: {sessionFolder}/{subfolder}\n";
                var placeholderData = System.Text.Encoding.UTF8.GetBytes(placeholderContent);

                try
                {
                    // Create a NEW HttpClient for each request to avoid header conflicts
                    using var httpClient = new HttpClient();
                    var serviceKey = _settings.ServiceKey.Trim();

                    // Direct REST API call to Supabase Storage
                    var uploadUrl = $"{_settings.Url}/storage/v1/object/{_settings.AttendanceSessionsBucket}/{placeholderPath}";

                    _logger.LogDebug("🔑 Using service key (first 20 chars): {Key}...", serviceKey.Substring(0, Math.Min(20, serviceKey.Length)));
                    _logger.LogDebug("🌐 Upload URL: {Url}", uploadUrl);

                    using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);

                    // Add headers one by one with validation bypass
                    request.Headers.TryAddWithoutValidation("apikey", serviceKey);
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {serviceKey}");

                    // Add content
                    request.Content = new ByteArrayContent(placeholderData);
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", "text/plain");
                    request.Content.Headers.TryAddWithoutValidation("x-upsert", "false");

                    var response = await httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        _logger.LogError("❌ Upload failed for {Subfolder}. Status: {Status}. Error: {Error}",
                            subfolder, response.StatusCode, errorBody);
                        continue;
                    }

                    _logger.LogDebug("✅ Created subfolder: {Subfolder}", subfolder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to create placeholder for {Subfolder}. Full error: {Message}",
                        subfolder, ex.ToString());
                    // Don't throw - continue trying other folders
                }
            }

            _logger.LogInformation("✅ Session folder structure created: {Folder}", sessionFolder);
            return sessionFolder;
        }, "CreateSessionFolder");
    }

    public async Task<string> UploadToSessionAsync(
        Guid sessionId,
        DateOnly sessionDate,
        byte[] fileData,
        string fileName,
        string subfolder,
        string contentType = "image/jpeg")
    {
        if (!IsEnabled())
        {
            throw new InvalidOperationException("Supabase storage is not enabled");
        }

        return await ExecuteWithRetryAsync(async () =>
        {
            var sessionFolder = GetSessionFolderPath(sessionId, sessionDate);
            var fullPath = $"{sessionFolder}/{subfolder}/{fileName}";

            _logger.LogInformation("📤 Uploading to session folder: {Path}", fullPath);

            var client = await _supabaseClient.Value;

            // Delete existing file if present
            try
            {
                await client.Storage
                    .From(_settings.AttendanceSessionsBucket)
                    .Remove(new List<string> { fullPath });
            }
            catch
            {
                // Ignore if file doesn't exist
            }

            // Upload new file
            await client.Storage
                .From(_settings.AttendanceSessionsBucket)
                .Upload(fileData, fullPath, new Supabase.Storage.FileOptions
                {
                    ContentType = contentType,
                    Upsert = true
                });

            var publicUrl = GetPublicUrl(fullPath);
            _logger.LogInformation("✅ Successfully uploaded to session: {Path}", fullPath);

            return publicUrl;
        }, "UploadToSession");
    }

    public async Task<byte[]> DownloadFromSessionAsync(
        Guid sessionId,
        DateOnly sessionDate,
        string fileName,
        string subfolder)
    {
        if (!IsEnabled())
        {
            throw new InvalidOperationException("Supabase storage is not enabled");
        }

        return await ExecuteWithRetryAsync(async () =>
        {
            var sessionFolder = GetSessionFolderPath(sessionId, sessionDate);
            var fullPath = $"{sessionFolder}/{subfolder}/{fileName}";

            _logger.LogInformation("📥 Downloading from session folder: {Path}", fullPath);

            var client = await _supabaseClient.Value;
            var fileData = await client.Storage
                .From(_settings.AttendanceSessionsBucket)
                .Download(fullPath, null);

            _logger.LogInformation("✅ Successfully downloaded: {Path} ({Size} bytes)",
                fullPath, fileData.Length);

            return fileData;
        }, "DownloadFromSession");
    }
    public async Task<List<string>> GetSessionFilesAsync(
        Guid sessionId,
        DateOnly sessionDate,
        string? subfolder = null)
    {
        if (!IsEnabled())
        {
            throw new InvalidOperationException("Supabase storage is not enabled");
        }

        return await ExecuteWithRetryAsync(async () =>
        {
            var sessionFolder = GetSessionFolderPath(sessionId, sessionDate);
            var searchPath = subfolder != null ? $"{sessionFolder}/{subfolder}" : sessionFolder;

            _logger.LogInformation("📂 Listing files in: {Path}", searchPath);

            var client = await _supabaseClient.Value;
            var files = await client.Storage
                .From(_settings.AttendanceSessionsBucket)
                .List(searchPath);

            var filePaths = (files ?? new List<Supabase.Storage.FileObject>())
                .Where(f => f.Name != ".keep") // Exclude placeholder files
                .Select(f => $"{searchPath}/{f.Name}")
                .ToList();

            _logger.LogInformation("✅ Found {Count} files in {Path}", filePaths.Count, searchPath);

            return filePaths;
        }, "GetSessionFiles");
    }

    public async Task<bool> DeleteSessionFolderAsync(Guid sessionId, DateOnly sessionDate)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("Supabase storage is not enabled, skipping delete");
            return false;
        }

        try
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var sessionFolder = GetSessionFolderPath(sessionId, sessionDate);
                _logger.LogInformation("🗑️ Deleting session folder: {Folder}", sessionFolder);

                // Get all files in session folder and subfolders
                var allFiles = new List<string>();
                var subfolders = new[] { "detected_faces", "embeddings", "results", "" };

                foreach (var subfolder in subfolders)
                {
                    try
                    {
                        var files = await GetSessionFilesAsync(sessionId, sessionDate,
                            string.IsNullOrEmpty(subfolder) ? null : subfolder);
                        allFiles.AddRange(files);
                    }
                    catch
                    {
                        // Continue if subfolder doesn't exist
                    }
                }

                if (allFiles.Any())
                {
                    var client = await _supabaseClient.Value;
                    await client.Storage
                        .From(_settings.AttendanceSessionsBucket)
                        .Remove(allFiles);

                    _logger.LogInformation("✅ Deleted {Count} files from session folder", allFiles.Count);
                }

                return true;
            }, "DeleteSessionFolder");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting session folder for {SessionId}", sessionId);
            return false;
        }
    }

    #endregion

    #region Helper Methods

    private string GetSessionFolderPath(Guid sessionId, DateOnly sessionDate)
    {
        var dateString = sessionDate.ToString("yyyy-MM-dd");
        return $"sessions/session_{sessionId}_{dateString}";
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

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, string operationName)
    {
        var context = new Polly.Context(operationName);
        return await _retryPolicy.ExecuteAsync(async (ctx) => await action(), context);
    }

    #endregion
}
