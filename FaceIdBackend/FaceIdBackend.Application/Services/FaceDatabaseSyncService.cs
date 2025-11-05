using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using Polly;
using Polly.Retry;

namespace FaceIdBackend.Application.Services;

/// <summary>
/// Service for synchronizing face database with Python Flask API
/// Handles preloading and cleanup of class face databases
/// </summary>
public class FaceDatabaseSyncService : IFaceDatabaseSyncService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FlaskApiSettings _flaskSettings;
    private readonly ILogger<FaceDatabaseSyncService> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public FaceDatabaseSyncService(
        IHttpClientFactory httpClientFactory,
        IOptions<FlaskApiSettings> flaskSettings,
        ILogger<FaceDatabaseSyncService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _flaskSettings = flaskSettings.Value;
        _logger = logger;

        // Configure retry policy with exponential backoff
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s due to: {Reason}",
                        retryCount,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "Unknown"
                    );
                });
    }

    /// <inheritdoc />
    public async Task<bool> PreloadClassDatabaseAsync(Guid classId)
    {
        try
        {
            _logger.LogInformation("📥 Preloading face database for class {ClassId}", classId);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_flaskSettings.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(120); // 2 minutes for large downloads

            var requestContent = JsonContent.Create(new
            {
                classId = classId.ToString()
            });

            var response = await _retryPolicy.ExecuteAsync(async () =>
                await httpClient.PostAsync("/api/database/sync", requestContent)
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FlaskSyncResponse>();

                if (result?.Success == true)
                {
                    _logger.LogInformation(
                        "✅ Successfully preloaded face database for class {ClassId}. Students: {StudentCount}. Message: {Message}",
                        classId,
                        result.StudentCount,
                        result.Message
                    );
                    return true;
                }
                else
                {
                    _logger.LogError(
                        "❌ Flask API reported failure while preloading class {ClassId}: {Error}",
                        classId,
                        result?.Error ?? "Unknown error"
                    );
                    return false;
                }
            }
            else
            {
                _logger.LogError(
                    "❌ HTTP error while preloading class {ClassId}: {StatusCode} - {ReasonPhrase}",
                    classId,
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
                "❌ Exception while preloading face database for class {ClassId}",
                classId
            );
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CleanupClassDatabaseAsync(Guid classId)
    {
        try
        {
            _logger.LogInformation("🗑️  Cleaning up face database for class {ClassId}", classId);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_flaskSettings.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var requestContent = JsonContent.Create(new
            {
                classId = classId.ToString()
            });

            var response = await _retryPolicy.ExecuteAsync(async () =>
                await httpClient.PostAsync("/api/database/cleanup", requestContent)
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FlaskCleanupResponse>();

                if (result?.Success == true)
                {
                    _logger.LogInformation(
                        "✅ Successfully cleaned up face database for class {ClassId}. Message: {Message}",
                        classId,
                        result.Message
                    );
                    return true;
                }
                else
                {
                    _logger.LogError(
                        "❌ Flask API reported failure while cleaning up class {ClassId}: {Error}",
                        classId,
                        result?.Error ?? "Unknown error"
                    );
                    return false;
                }
            }
            else
            {
                _logger.LogError(
                    "❌ HTTP error while cleaning up class {ClassId}: {StatusCode} - {ReasonPhrase}",
                    classId,
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
                "❌ Exception while cleaning up face database for class {ClassId}",
                classId
            );
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsFlaskApiHealthyAsync()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_flaskSettings.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var response = await httpClient.GetAsync("/health");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FlaskHealthResponse>();
                return result?.Status == "healthy";
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Flask API health check failed: {Message}", ex.Message);
            return false;
        }
    }

    #region Response Models

    private class FlaskSyncResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int StudentCount { get; set; }
        public string? ClassId { get; set; }
        public string? Error { get; set; }
    }

    private class FlaskCleanupResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? DeletedPath { get; set; }
        public string? Error { get; set; }
    }

    private class FlaskHealthResponse
    {
        public string? Status { get; set; }
        public string? Service { get; set; }
        public string? Version { get; set; }
    }

    #endregion
}
