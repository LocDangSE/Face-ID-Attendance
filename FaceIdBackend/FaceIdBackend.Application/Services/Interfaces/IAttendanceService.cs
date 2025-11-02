using FaceIdBackend.Shared.DTOs;
using Microsoft.AspNetCore.Http;

namespace FaceIdBackend.Application.Services.Interfaces;

public interface IAttendanceService
{
    Task<CreateSessionResponse> CreateAttendanceSessionAsync(CreateSessionRequest request);
    Task<List<AttendanceSessionDto>> GetAllSessionsAsync();
    Task<SessionDetailsDto> GetSessionDetailsAsync(Guid sessionId);
    Task<CompleteSessionResponse> CompleteSessionAsync(Guid sessionId);
    Task<List<AttendanceSessionDto>> GetActiveSessionsAsync();
    Task<RecognitionResponse> RecognizeFaceAndMarkAttendanceAsync(Guid sessionId, IFormFile image);
    Task<ApiResponse<string>> ManualMarkAttendanceAsync(ManualAttendanceRequest request);
    Task<AttendanceReportDto> GetAttendanceReportAsync(Guid classId, DateOnly dateFrom, DateOnly dateTo, Guid? studentId = null);
    Task<StudentHistoryDto> GetStudentAttendanceHistoryAsync(Guid studentId, Guid classId);
    Task<SessionDetailsDto> GetSessionAttendanceDetailsAsync(Guid sessionId);
    Task<byte[]> ExportAttendanceToExcelAsync(Guid classId, DateOnly dateFrom, DateOnly dateTo);
}
