using FaceIdBackend.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FaceIdBackend.Controllers;

/// <summary>
/// Controller for managing attendance session lifecycle
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(ISessionService sessionService, ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new attendance session
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSession(
        [FromQuery] Guid classId,
        [FromQuery] DateOnly sessionDate,
        [FromQuery] string? location = null)
    {
        try
        {
            var session = await _sessionService.CreateSessionAsync(classId, sessionDate, location);
            return Ok(new
            {
                success = true,
                message = "Session created successfully",
                data = session
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Complete an attendance session
    /// </summary>
    [HttpPost("{sessionId}/complete")]
    public async Task<IActionResult> CompleteSession(Guid sessionId)
    {
        try
        {
            await _sessionService.CompleteSessionAsync(sessionId);
            return Ok(new
            {
                success = true,
                message = "Session completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing session {SessionId}", sessionId);
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Delete an attendance session
    /// </summary>
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> DeleteSession(Guid sessionId)
    {
        try
        {
            await _sessionService.DeleteSessionAsync(sessionId);
            return Ok(new
            {
                success = true,
                message = "Session deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get session details with Supabase URLs
    /// </summary>
    [HttpGet("{sessionId}")]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        try
        {
            var session = await _sessionService.GetSessionWithUrlsAsync(sessionId);
            return Ok(new
            {
                success = true,
                data = session
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session {SessionId}", sessionId);
            return NotFound(new
            {
                success = false,
                message = ex.Message
            });
        }
    }
}
