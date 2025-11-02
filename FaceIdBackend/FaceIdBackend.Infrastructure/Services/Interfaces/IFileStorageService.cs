using Microsoft.AspNetCore.Http;

namespace FaceIdBackend.Infrastructure.Services.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveStudentPhotoAsync(IFormFile file, Guid studentId);
    Task DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
    Stream GetFileStream(string filePath);
}
