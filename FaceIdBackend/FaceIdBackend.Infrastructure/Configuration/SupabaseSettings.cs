namespace FaceIdBackend.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Supabase storage service
/// Enhanced with retry logic and timeout settings
/// </summary>
public class SupabaseSettings
{
    /// <summary>
    /// Supabase project URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Supabase service key for authentication
    /// </summary>
    public string ServiceKey { get; set; } = string.Empty;

    /// <summary>
    /// Storage bucket for student profile photos
    /// </summary>
    public string StudentPhotosBucket { get; set; } = "student-photos";

    /// <summary>
    /// Storage bucket for attendance session data (detected faces, embeddings, results)
    /// </summary>
    public string AttendanceSessionsBucket { get; set; } = "attendance-sessions";

    /// <summary>
    /// Legacy property for backward compatibility - points to AttendanceSessionsBucket
    /// </summary>
    [Obsolete("Use StudentPhotosBucket or AttendanceSessionsBucket instead")]
    public string StorageBucket
    {
        get => AttendanceSessionsBucket;
        set => AttendanceSessionsBucket = value;
    }

    /// <summary>
    /// Format for generating public URLs: {url}/storage/v1/object/public/{bucket}/{path}
    /// </summary>
    public string PublicUrlFormat { get; set; } = "{0}/storage/v1/object/public/{1}/{2}";

    /// <summary>
    /// Enable/disable Supabase storage
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Legacy Key property for backward compatibility
    /// </summary>
    public string Key
    {
        get => ServiceKey;
        set => ServiceKey = value;
    }
}
