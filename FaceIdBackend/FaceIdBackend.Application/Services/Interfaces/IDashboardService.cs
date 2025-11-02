using FaceIdBackend.Shared.DTOs;

namespace FaceIdBackend.Application.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<ClassStatisticsDto> GetClassStatisticsAsync(Guid classId, DateOnly dateFrom, DateOnly dateTo);
}
