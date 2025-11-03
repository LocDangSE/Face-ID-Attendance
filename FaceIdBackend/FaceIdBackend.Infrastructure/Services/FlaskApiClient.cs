using FaceIdBackend.Infrastructure.Configuration;
using FaceIdBackend.Infrastructure.Services.Interfaces;
using FaceIdBackend.Shared.DTOs.Flask;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FaceIdBackend.Infrastructure.Services;

public interface IFlaskApiClient
{
    Task<FlaskRegisterResponse> RegisterStudentAsync(Guid studentId, IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<FlaskRecognizeResponse> AnalyzeFacesAsync(Guid classId, IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}

public class FlaskApiClient : IFlaskApiClient
{
    private readonly HttpClient _httpClient;
    private readonly FlaskApiSettings _settings;
    private readonly ILogger<FlaskApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly AsyncRetryPolicy _retryPolicy;

    public FlaskApiClient(HttpClient httpClient, IOptions<FlaskApiSettings> settings, ILogger<FlaskApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(_settings.MaxRetries, retryAttempt => TimeSpan.FromMilliseconds(_settings.RetryDelayMs),
            onRetry: (ex, ts, count, ctx) =>
            {
                _logger.LogWarning("Retry {Attempt} for Flask API due to {Error}", count, ex.Message);
            });
    }

    public async Task<FlaskRegisterResponse> RegisterStudentAsync(Guid studentId, IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var content = new MultipartFormDataContent();
            using var stream = imageFile.OpenReadStream();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
            content.Add(streamContent, "image", imageFile.FileName);
            content.Add(new StringContent(studentId.ToString()), "studentId");

            if (_settings.LogRequests)
                _logger.LogDebug("Flask RegisterStudent request: studentId={StudentId}, filename={FileName}", studentId, imageFile.FileName);

            var response = await _httpClient.PostAsync("/api/face/register", content, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (_settings.LogResponses)
                _logger.LogDebug("Flask RegisterStudent response: {Response}", json);

            response.EnsureSuccessStatusCode();

            var result = JsonSerializer.Deserialize<FlaskRegisterResponse>(json, _jsonOptions)
                         ?? throw new InvalidOperationException("Failed to deserialize Flask register response");

            return result;
        });
    }

    public async Task<FlaskRecognizeResponse> AnalyzeFacesAsync(Guid classId, IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var content = new MultipartFormDataContent();
            using var stream = imageFile.OpenReadStream();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
            content.Add(streamContent, "image", imageFile.FileName);
            content.Add(new StringContent(classId.ToString()), "classId");

            if (_settings.LogRequests)
                _logger.LogDebug("Flask AnalyzeFaces request: classId={ClassId}, filename={FileName}", classId, imageFile.FileName);

            var response = await _httpClient.PostAsync("/api/face/recognize", content, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Always log the response for debugging
            _logger.LogInformation("📥 Flask RAW response: {Response}", json);

            response.EnsureSuccessStatusCode();

            var result = JsonSerializer.Deserialize<FlaskRecognizeResponse>(json, _jsonOptions)
                         ?? throw new InvalidOperationException("Failed to deserialize Flask recognize response");

            // Log the deserialized result for debugging
            _logger.LogInformation("🔍 Deserialized Flask response - Success: {Success}, TotalFaces: {TotalFaces}, RecognizedCount: {RecognizedCount}",
                result.Success, result.TotalFacesDetected, result.RecognizedStudents?.Count ?? 0);

            return result;
        });
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Flask health check failed");
            return false;
        }
    }
}
