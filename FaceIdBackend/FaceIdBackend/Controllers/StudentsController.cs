using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace FaceIdBackend.API.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all students
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<StudentDto>>>> GetAllStudents()
    {
        try
        {
            var students = await _studentService.GetAllStudentsAsync();
            return Ok(new ApiResponse<List<StudentDto>>
            {
                Success = true,
                Message = "Students retrieved successfully",
                Data = students
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all students");
            return StatusCode(500, new ApiResponse<List<StudentDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving students",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get student by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> GetStudentById(Guid id)
    {
        try
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound(new ApiResponse<StudentDto>
                {
                    Success = false,
                    Message = $"Student with ID {id} not found"
                });
            }

            return Ok(new ApiResponse<StudentDto>
            {
                Success = true,
                Message = "Student retrieved successfully",
                Data = student
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student {StudentId}", id);
            return StatusCode(500, new ApiResponse<StudentDto>
            {
                Success = false,
                Message = "An error occurred while retrieving student",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Create new student with photo
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudentDto>>> CreateStudent(
        [FromForm] CreateStudentRequest request)
    {
        try
        {
            if (request.Photo == null || request.Photo.Length == 0)
            {
                return BadRequest(new ApiResponse<StudentDto>
                {
                    Success = false,
                    Message = "Photo is required"
                });
            }

            var student = await _studentService.CreateStudentAsync(request, request.Photo);
            return CreatedAtAction(nameof(GetStudentById), new { id = student.StudentId },
                new ApiResponse<StudentDto>
                {
                    Success = true,
                    Message = "Student created successfully",
                    Data = student
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<StudentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            return StatusCode(500, new ApiResponse<StudentDto>
            {
                Success = false,
                Message = "An error occurred while creating student",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update student information
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> UpdateStudent(
        Guid id,
        [FromBody] UpdateStudentRequest request)
    {
        try
        {
            var student = await _studentService.UpdateStudentAsync(id, request);
            return Ok(new ApiResponse<StudentDto>
            {
                Success = true,
                Message = "Student updated successfully",
                Data = student
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<StudentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<StudentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student {StudentId}", id);
            return StatusCode(500, new ApiResponse<StudentDto>
            {
                Success = false,
                Message = "An error occurred while updating student",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Delete student
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteStudent(Guid id)
    {
        try
        {
            await _studentService.DeleteStudentAsync(id);
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Student deleted successfully",
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
            _logger.LogError(ex, "Error deleting student {StudentId}", id);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while deleting student",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update student photo
    /// </summary>
    [HttpPut("{id}/photo")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> UpdateStudentPhoto(
        Guid id,
        [FromForm] IFormFile photo)
    {
        try
        {
            if (photo == null || photo.Length == 0)
            {
                return BadRequest(new ApiResponse<StudentDto>
                {
                    Success = false,
                    Message = "Photo is required"
                });
            }

            var student = await _studentService.UpdateStudentPhotoAsync(id, photo);
            return Ok(new ApiResponse<StudentDto>
            {
                Success = true,
                Message = "Student photo updated successfully",
                Data = student
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<StudentDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student photo {StudentId}", id);
            return StatusCode(500, new ApiResponse<StudentDto>
            {
                Success = false,
                Message = "An error occurred while updating student photo",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Delete student photo
    /// </summary>
    [HttpDelete("{id}/photo")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteStudentPhoto(Guid id)
    {
        try
        {
            await _studentService.DeleteStudentPhotoAsync(id);
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Student photo deleted successfully",
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
            _logger.LogError(ex, "Error deleting student photo {StudentId}", id);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while deleting student photo",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
