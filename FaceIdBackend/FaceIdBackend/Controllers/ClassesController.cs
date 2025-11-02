using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace FaceIdBackend.API.Controllers;

[ApiController]
[Route("api/classes")]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classService;
    private readonly ILogger<ClassesController> _logger;

    public ClassesController(IClassService classService, ILogger<ClassesController> logger)
    {
        _classService = classService;
        _logger = logger;
    }

    /// <summary>
    /// Get all classes
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ClassDto>>>> GetAllClasses()
    {
        try
        {
            var classes = await _classService.GetAllClassesAsync();
            return Ok(new ApiResponse<List<ClassDto>>
            {
                Success = true,
                Message = "Classes retrieved successfully",
                Data = classes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all classes");
            return StatusCode(500, new ApiResponse<List<ClassDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving classes",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get class by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ClassDto>>> GetClassById(Guid id)
    {
        try
        {
            var classDto = await _classService.GetClassByIdAsync(id);
            if (classDto == null)
            {
                return NotFound(new ApiResponse<ClassDto>
                {
                    Success = false,
                    Message = $"Class with ID {id} not found"
                });
            }

            return Ok(new ApiResponse<ClassDto>
            {
                Success = true,
                Message = "Class retrieved successfully",
                Data = classDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting class {ClassId}", id);
            return StatusCode(500, new ApiResponse<ClassDto>
            {
                Success = false,
                Message = "An error occurred while retrieving class",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Create new class
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ClassDto>>> CreateClass([FromBody] CreateClassRequest request)
    {
        try
        {
            var classDto = await _classService.CreateClassAsync(request);
            return CreatedAtAction(nameof(GetClassById), new { id = classDto.ClassId },
                new ApiResponse<ClassDto>
                {
                    Success = true,
                    Message = "Class created successfully",
                    Data = classDto
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<ClassDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating class");
            return StatusCode(500, new ApiResponse<ClassDto>
            {
                Success = false,
                Message = "An error occurred while creating class",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update class information
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ClassDto>>> UpdateClass(
        Guid id,
        [FromBody] UpdateClassRequest request)
    {
        try
        {
            var classDto = await _classService.UpdateClassAsync(id, request);
            return Ok(new ApiResponse<ClassDto>
            {
                Success = true,
                Message = "Class updated successfully",
                Data = classDto
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ClassDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<ClassDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating class {ClassId}", id);
            return StatusCode(500, new ApiResponse<ClassDto>
            {
                Success = false,
                Message = "An error occurred while updating class",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Delete class
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteClass(Guid id)
    {
        try
        {
            await _classService.DeleteClassAsync(id);
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Class deleted successfully",
                Data = id.ToString()
            });
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
            _logger.LogError(ex, "Error deleting class {ClassId}", id);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while deleting class",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get all students enrolled in a class
    /// </summary>
    [HttpGet("{id}/students")]
    public async Task<ActionResult<ApiResponse<List<EnrolledStudentDto>>>> GetClassStudents(Guid id)
    {
        try
        {
            var students = await _classService.GetClassStudentsAsync(id);
            return Ok(new ApiResponse<List<EnrolledStudentDto>>
            {
                Success = true,
                Message = "Class students retrieved successfully",
                Data = students
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting students for class {ClassId}", id);
            return StatusCode(500, new ApiResponse<List<EnrolledStudentDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving class students",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Enroll students in a class
    /// </summary>
    [HttpPost("{id}/enroll")]
    public async Task<ActionResult<ApiResponse<string>>> EnrollStudents(
        Guid id,
        [FromBody] EnrollStudentsRequest request)
    {
        try
        {
            var result = await _classService.EnrollStudentsAsync(id, request);
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
            _logger.LogError(ex, "Error enrolling students in class {ClassId}", id);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while enrolling students",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Remove student from class
    /// </summary>
    [HttpDelete("{classId}/students/{studentId}")]
    public async Task<ActionResult<ApiResponse<string>>> RemoveStudentFromClass(
        Guid classId,
        Guid studentId)
    {
        try
        {
            await _classService.RemoveStudentFromClassAsync(classId, studentId);
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Student removed from class successfully",
                Data = studentId.ToString()
            });
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
            _logger.LogError(ex, "Error removing student {StudentId} from class {ClassId}", studentId, classId);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while removing student from class",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
