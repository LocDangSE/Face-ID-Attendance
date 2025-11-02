namespace FaceIdBackend.Infrastructure.Configuration;

public class SupabaseSettings
{
    public string Url { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string StorageBucket { get; set; } = "student-image";
    public string PublicUrlFormat { get; set; } = "{0}/storage/v1/object/public/{1}/{2}";
    public bool Enabled { get; set; } = false;
}
