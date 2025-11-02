using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace FaceIdBackend.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetStats()
    {
        try
        {
            var stats = await _dashboardService.GetDashboardStatsAsync();
            return Ok(new ApiResponse<DashboardStatsDto>
            {
                Success = true,
                Message = "Dashboard statistics retrieved successfully",
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return StatusCode(500, new ApiResponse<DashboardStatsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving dashboard statistics",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get recent sessions
    /// </summary>
    [HttpGet("recent-sessions")]
    public async Task<ActionResult<ApiResponse<List<AttendanceSessionDto>>>> GetRecentSessions([FromQuery] int limit = 5)
    {
        try
        {
            // This will need to be implemented in the service
            // For now, return empty list
            return Ok(new ApiResponse<List<AttendanceSessionDto>>
            {
                Success = true,
                Message = "Recent sessions retrieved successfully",
                Data = new List<AttendanceSessionDto>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent sessions");
            return StatusCode(500, new ApiResponse<List<AttendanceSessionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving recent sessions",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get today's attendance
    /// </summary>
    [HttpGet("today-attendance")]
    public async Task<ActionResult<ApiResponse<int>>> GetTodayAttendance()
    {
        try
        {
            // This will need to be implemented in the service
            return Ok(new ApiResponse<int>
            {
                Success = true,
                Message = "Today's attendance retrieved successfully",
                Data = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting today's attendance");
            return StatusCode(500, new ApiResponse<int>
            {
                Success = false,
                Message = "An error occurred while retrieving today's attendance",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
