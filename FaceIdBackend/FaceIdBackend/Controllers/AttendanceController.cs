using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace FaceIdBackend.API.Controllers;

[ApiController]
[Route("api/attendance")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(
        IAttendanceService attendanceService,
        IDashboardService dashboardService,
        ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get all attendance sessions
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<List<AttendanceSessionDto>>>> GetAllSessions()
    {
        try
        {
            var sessions = await _attendanceService.GetAllSessionsAsync();
            return Ok(new ApiResponse<List<AttendanceSessionDto>>
            {
                Success = true,
                Message = "Sessions retrieved successfully",
                Data = sessions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all sessions");
            return StatusCode(500, new ApiResponse<List<AttendanceSessionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving sessions",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Create new attendance session
    /// </summary>
    [HttpPost("sessions")]
    public async Task<ActionResult<ApiResponse<CreateSessionResponse>>> CreateSession(
        [FromBody] CreateSessionRequest request)
    {
        try
        {
            var session = await _attendanceService.CreateAttendanceSessionAsync(request);
            return Ok(new ApiResponse<CreateSessionResponse>
            {
                Success = true,
                Message = "Attendance session created successfully",
                Data = session
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<CreateSessionResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<CreateSessionResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attendance session");
            return StatusCode(500, new ApiResponse<CreateSessionResponse>
            {
                Success = false,
                Message = "An error occurred while creating attendance session",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get session details
    /// </summary>
    [HttpGet("sessions/{sessionId}")]
    public async Task<ActionResult<ApiResponse<SessionDetailsDto>>> GetSessionDetails(Guid sessionId)
    {
        try
        {
            var session = await _attendanceService.GetSessionDetailsAsync(sessionId);
            return Ok(new ApiResponse<SessionDetailsDto>
            {
                Success = true,
                Message = "Session details retrieved successfully",
                Data = session
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<SessionDetailsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session details {SessionId}", sessionId);
            return StatusCode(500, new ApiResponse<SessionDetailsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving session details",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Complete attendance session
    /// </summary>
    [HttpPut("sessions/{sessionId}/complete")]
    public async Task<ActionResult<ApiResponse<CompleteSessionResponse>>> CompleteSession(Guid sessionId)
    {
        try
        {
            var result = await _attendanceService.CompleteSessionAsync(sessionId);
            return Ok(new ApiResponse<CompleteSessionResponse>
            {
                Success = true,
                Message = "Session completed successfully",
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<CompleteSessionResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<CompleteSessionResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing session {SessionId}", sessionId);
            return StatusCode(500, new ApiResponse<CompleteSessionResponse>
            {
                Success = false,
                Message = "An error occurred while completing session",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get active sessions
    /// </summary>
    [HttpGet("sessions/active")]
    public async Task<ActionResult<ApiResponse<List<AttendanceSessionDto>>>> GetActiveSessions()
    {
        try
        {
            var sessions = await _attendanceService.GetActiveSessionsAsync();
            return Ok(new ApiResponse<List<AttendanceSessionDto>>
            {
                Success = true,
                Message = "Active sessions retrieved successfully",
                Data = sessions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions");
            return StatusCode(500, new ApiResponse<List<AttendanceSessionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving active sessions",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Recognize face and mark attendance
    /// </summary>
    [HttpPost("recognize")]
    public async Task<ActionResult<RecognitionResponse>> RecognizeFace(
        [FromForm] RecognizeStudentRequest request)
    {
        try
        {
            if (request.Image == null || request.Image.Length == 0)
            {
                return BadRequest(new RecognitionResponse
                {
                    Success = false,
                    Message = "Image is required",
                    RecognizedStudents = new List<RecognizedStudentDto>()
                });
            }

            var result = await _attendanceService.RecognizeFaceAndMarkAttendanceAsync(request.SessionId, request.Image);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new RecognitionResponse
            {
                Success = false,
                Message = ex.Message,
                RecognizedStudents = new List<RecognizedStudentDto>()
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new RecognitionResponse
            {
                Success = false,
                Message = ex.Message,
                RecognizedStudents = new List<RecognizedStudentDto>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recognizing face for session {SessionId}", request.SessionId);
            return StatusCode(500, new RecognitionResponse
            {
                Success = false,
                Message = "An error occurred during face recognition",
                RecognizedStudents = new List<RecognizedStudentDto>()
            });
        }
    }

    /// <summary>
    /// Manual mark attendance
    /// </summary>
    [HttpPost("mark")]
    public async Task<ActionResult<ApiResponse<string>>> ManualMarkAttendance(
        [FromBody] ManualAttendanceRequest request)
    {
        try
        {
            var result = await _attendanceService.ManualMarkAttendanceAsync(request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<string>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error manually marking attendance");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while marking attendance",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get attendance report
    /// </summary>
    [HttpGet("reports")]
    public async Task<ActionResult<ApiResponse<AttendanceReportDto>>> GetAttendanceReport(
        [FromQuery] Guid classId,
        [FromQuery] DateOnly dateFrom,
        [FromQuery] DateOnly dateTo,
        [FromQuery] Guid? studentId = null)
    {
        try
        {
            var report = await _attendanceService.GetAttendanceReportAsync(classId, dateFrom, dateTo, studentId);
            return Ok(new ApiResponse<AttendanceReportDto>
            {
                Success = true,
                Message = "Attendance report retrieved successfully",
                Data = report
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<AttendanceReportDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance report for class {ClassId}", classId);
            return StatusCode(500, new ApiResponse<AttendanceReportDto>
            {
                Success = false,
                Message = "An error occurred while retrieving attendance report",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Export attendance report to Excel
    /// </summary>
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportAttendanceToExcel(
        [FromQuery] Guid classId,
        [FromQuery] DateOnly dateFrom,
        [FromQuery] DateOnly dateTo)
    {
        try
        {
            var excelData = await _attendanceService.ExportAttendanceToExcelAsync(classId, dateFrom, dateTo);
            var fileName = $"Attendance_{classId}_{dateFrom:yyyyMMdd}_{dateTo:yyyyMMdd}.xlsx";

            return File(excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<string>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting attendance to Excel for class {ClassId}", classId);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while exporting attendance",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get student attendance history
    /// </summary>
    [HttpGet("students/{studentId}/history")]
    public async Task<ActionResult<ApiResponse<StudentHistoryDto>>> GetStudentHistory(
        Guid studentId,
        [FromQuery] Guid classId)
    {
        try
        {
            var history = await _attendanceService.GetStudentAttendanceHistoryAsync(studentId, classId);
            return Ok(new ApiResponse<StudentHistoryDto>
            {
                Success = true,
                Message = "Student attendance history retrieved successfully",
                Data = history
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<StudentHistoryDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student history {StudentId} for class {ClassId}", studentId, classId);
            return StatusCode(500, new ApiResponse<StudentHistoryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving student history",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get session attendance details
    /// </summary>
    [HttpGet("sessions/{sessionId}/details")]
    public async Task<ActionResult<ApiResponse<SessionDetailsDto>>> GetSessionAttendanceDetails(Guid sessionId)
    {
        try
        {
            var details = await _attendanceService.GetSessionAttendanceDetailsAsync(sessionId);
            return Ok(new ApiResponse<SessionDetailsDto>
            {
                Success = true,
                Message = "Session attendance details retrieved successfully",
                Data = details
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<SessionDetailsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session attendance details {SessionId}", sessionId);
            return StatusCode(500, new ApiResponse<SessionDetailsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving session details",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("dashboard/stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
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
}
