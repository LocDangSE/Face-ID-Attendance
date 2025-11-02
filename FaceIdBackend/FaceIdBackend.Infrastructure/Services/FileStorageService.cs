using FaceIdBackend.Infrastructure.Configuration;
using FaceIdBackend.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FaceIdBackend.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly FileStorageSettings _settings;
    private readonly string _baseUploadPath;

    public FileStorageService(IOptions<FileStorageSettings> settings)
    {
        _settings = settings.Value;
        _baseUploadPath = Path.Combine(Directory.GetCurrentDirectory(), _settings.UploadPath);

        // Ensure upload directory exists
        if (!Directory.Exists(_baseUploadPath))
        {
            Directory.CreateDirectory(_baseUploadPath);
        }

        var studentPhotosPath = Path.Combine(_baseUploadPath, _settings.StudentPhotosPath);
        if (!Directory.Exists(studentPhotosPath))
        {
            Directory.CreateDirectory(studentPhotosPath);
        }
    }

    public async Task<string> SaveStudentPhotoAsync(IFormFile file, Guid studentId)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        if (file.Length > _settings.MaxFileSizeBytes)
            throw new ArgumentException($"File size exceeds maximum allowed size of {_settings.MaxFileSizeBytes / 1024 / 1024}MB");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_settings.AllowedExtensions.Contains(extension))
            throw new ArgumentException($"File type {extension} is not allowed");

        var fileName = $"{studentId}{extension}";
        var studentPhotosPath = Path.Combine(_baseUploadPath, _settings.StudentPhotosPath);
        var filePath = Path.Combine(studentPhotosPath, fileName);

        // Delete old photo if exists
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return absolute physical path for Flask integration
        return filePath;
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.CompletedTask;

        // Convert URL path to physical path
        var physicalPath = ConvertUrlToPhysicalPath(filePath);

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult(false);

        var physicalPath = ConvertUrlToPhysicalPath(filePath);
        return Task.FromResult(File.Exists(physicalPath));
    }

    public Stream GetFileStream(string filePath)
    {
        var physicalPath = ConvertUrlToPhysicalPath(filePath);

        if (!File.Exists(physicalPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        return new FileStream(physicalPath, FileMode.Open, FileAccess.Read);
    }

    private string ConvertUrlToPhysicalPath(string urlPath)
    {
        // Remove leading slash and convert URL path to physical path
        var relativePath = urlPath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        return Path.Combine(Directory.GetCurrentDirectory(), relativePath);
    }
}
