using FaceIdBackend.Shared.DTOs;
using Microsoft.AspNetCore.Http;

namespace FaceIdBackend.Application.Services.Interfaces;

public interface IStudentService
{
    Task<List<StudentDto>> GetAllStudentsAsync();
    Task<StudentDto?> GetStudentByIdAsync(Guid id);
    Task<StudentDto> CreateStudentAsync(CreateStudentRequest request, IFormFile photo);
    Task<StudentDto> UpdateStudentAsync(Guid id, UpdateStudentRequest request);
    Task DeleteStudentAsync(Guid id);
    Task<StudentDto> UpdateStudentPhotoAsync(Guid id, IFormFile photo);
    Task DeleteStudentPhotoAsync(Guid id);
}
