namespace FaceIdBackend.Infrastructure.Configuration;

public class FileStorageSettings
{
    public string UploadPath { get; set; } = "wwwroot/uploads";
    public string StudentPhotosPath { get; set; } = "students";
    public long MaxFileSizeBytes { get; set; } = 5242880; // 5MB
    public List<string> AllowedExtensions { get; set; } = new() { ".jpg", ".jpeg", ".png" };
}
