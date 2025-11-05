using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Infrastructure.Configuration;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Net.Http.Json;
using System.Text.Json;

namespace FaceIdBackend.Application.Services;

/// <summary>
/// Service for synchronizing Python face recognition cache with database changes
/// Uses Hangfire for background job processing to prevent blocking CRUD operations
/// </summary>
public class CacheSyncService : ICacheSyncService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FlaskApiSettings _flaskSettings;
    private readonly ILogger<CacheSyncService> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public CacheSyncService(
        IHttpClientFactory httpClientFactory,
        IOptions<FlaskApiSettings> flaskSettings,
        ILogger<CacheSyncService> logger,
        IBackgroundJobClient backgroundJobClient)
    {
        _httpClientFactory = httpClientFactory;
        _flaskSettings = flaskSettings.Value;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;

        // Configure retry policy
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Cache sync retry {RetryCount} after {Delay}s due to: {Reason}",
                        retryCount,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "Unknown"
                    );
                });
    }

    /// <inheritdoc />
    public async Task<bool> ClearStudentCacheAsync(Guid studentId)
    {
        try
        {
            _logger.LogInformation("🗑️  Clearing cache for student {StudentId}", studentId);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_flaskSettings.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var requestContent = JsonContent.Create(new
            {
                studentId = studentId.ToString()
            });

            var response = await _retryPolicy.ExecuteAsync(async () =>
                await httpClient.PostAsync("/api/cache/clear", requestContent)
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FlaskCacheResponse>();

                if (result?.Success == true)
                {
                    _logger.LogInformation(
                        "✅ Successfully cleared cache for student {StudentId}. Message: {Message}",
                        studentId,
                        result.Message
                    );
                    return true;
                }
                else
                {
                    _logger.LogError(
                        "❌ Flask API reported failure while clearing cache for student {StudentId}: {Error}",
                        studentId,
                        result?.Error ?? "Unknown error"
                    );
                    return false;
                }
            }
            else
            {
                _logger.LogError(
                    "❌ HTTP error while clearing cache for student {StudentId}: {StatusCode} - {ReasonPhrase}",
                    studentId,
                    response.StatusCode,
                    response.ReasonPhrase
                );
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Exception while clearing cache for student {StudentId}",
                studentId
            );
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RefreshStudentCacheAsync(Guid studentId, string photoUrl)
    {
        try
        {
            _logger.LogInformation("🔄 Refreshing cache for student {StudentId} with photo: {PhotoUrl}", studentId, photoUrl);

            // For now, clear the cache for this student
            // In the future, we could implement re-registration via Flask API
            // This would require downloading the photo and re-uploading to Flask

            var cleared = await ClearStudentCacheAsync(studentId);

            if (cleared)
            {
                _logger.LogInformation(
                    "✅ Cache refreshed for student {StudentId}. " +
                    "Note: Cache cleared - face will be re-registered on next database sync.",
                    studentId
                );
            }

            return cleared;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Exception while refreshing cache for student {StudentId}",
                studentId
            );
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ClearAllCacheAsync()
    {
        try
        {
            _logger.LogInformation("🗑️  Clearing all cache");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_flaskSettings.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var requestContent = JsonContent.Create(new { });

            var response = await _retryPolicy.ExecuteAsync(async () =>
                await httpClient.PostAsync("/api/cache/clear", requestContent)
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FlaskCacheResponse>();

                if (result?.Success == true)
                {
                    _logger.LogInformation("✅ Successfully cleared all cache. Message: {Message}", result.Message);
                    return true;
                }
                else
                {
                    _logger.LogError("❌ Flask API reported failure while clearing all cache: {Error}",
                        result?.Error ?? "Unknown error");
                    return false;
                }
            }
            else
            {
                _logger.LogError(
                    "❌ HTTP error while clearing all cache: {StatusCode} - {ReasonPhrase}",
                    response.StatusCode,
                    response.ReasonPhrase
                );
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception while clearing all cache");
            return false;
        }
    }

    /// <inheritdoc />
    public string EnqueueClearStudentCache(Guid studentId)
    {
        try
        {
            var jobId = _backgroundJobClient.Enqueue<ICacheSyncService>(
                service => service.ClearStudentCacheAsync(studentId)
            );

            _logger.LogInformation(
                "📅 Enqueued cache clear job for student {StudentId}. JobId: {JobId}",
                studentId,
                jobId
            );

            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed to enqueue cache clear job for student {StudentId}",
                studentId
            );
            throw;
        }
    }

    /// <inheritdoc />
    public string EnqueueRefreshStudentCache(Guid studentId, string photoUrl)
    {
        try
        {
            var jobId = _backgroundJobClient.Enqueue<ICacheSyncService>(
                service => service.RefreshStudentCacheAsync(studentId, photoUrl)
            );

            _logger.LogInformation(
                "📅 Enqueued cache refresh job for student {StudentId}. JobId: {JobId}",
                studentId,
                jobId
            );

            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed to enqueue cache refresh job for student {StudentId}",
                studentId
            );
            throw;
        }
    }

    #region Response Models

    private class FlaskCacheResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    #endregion
}
