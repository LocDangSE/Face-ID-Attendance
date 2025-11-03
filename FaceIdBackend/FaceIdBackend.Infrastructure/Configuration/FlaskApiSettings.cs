namespace FaceIdBackend.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Flask Face Recognition API
/// Enhanced with retry and logging settings
/// </summary>
public class FlaskApiSettings
{
    /// <summary>
    /// Base URL of the Flask API service
    /// Example: http://localhost:5000 or http://192.168.1.100:5000
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Request timeout in seconds
    /// Default: 120 seconds (face recognition can take time for multiple faces)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Maximum retry attempts for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retries in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Enable request logging for debugging
    /// </summary>
    public bool LogRequests { get; set; } = true;

    /// <summary>
    /// Enable response logging for debugging
    /// </summary>
    public bool LogResponses { get; set; } = true;

    /// <summary>
    /// Enable health check on startup (backward compatibility)
    /// </summary>
    public bool EnableHealthCheck { get; set; } = true;

    /// <summary>
    /// Legacy RetryCount property for backward compatibility
    /// </summary>
    public int RetryCount
    {
        get => MaxRetries;
        set => MaxRetries = value;
    }
}
