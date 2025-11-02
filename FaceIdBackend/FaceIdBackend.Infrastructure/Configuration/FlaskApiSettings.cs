namespace FaceIdBackend.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Flask Face Recognition API
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
    /// Default: 30 seconds (face recognition can take time)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable health check on startup
    /// </summary>
    public bool EnableHealthCheck { get; set; } = true;

    /// <summary>
    /// Retry count for failed requests
    /// </summary>
    public int RetryCount { get; set; } = 3;
}
