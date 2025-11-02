using FaceIdBackend.Shared.DTOs;

namespace FaceIdBackend.Application.Services.Interfaces;

public interface IClassService
{
    Task<List<ClassDto>> GetAllClassesAsync();
    Task<ClassDto?> GetClassByIdAsync(Guid id);
    Task<ClassDto> CreateClassAsync(CreateClassRequest request);
    Task<ClassDto> UpdateClassAsync(Guid id, UpdateClassRequest request);
    Task DeleteClassAsync(Guid id);
    Task<List<EnrolledStudentDto>> GetClassStudentsAsync(Guid classId);
    Task<ApiResponse<string>> EnrollStudentsAsync(Guid classId, EnrollStudentsRequest request);
    Task RemoveStudentFromClassAsync(Guid classId, Guid studentId);
}
