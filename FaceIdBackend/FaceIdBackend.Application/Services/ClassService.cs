using AutoMapper;
using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Domain.Data;
using FaceIdBackend.Infrastructure.Services.Interfaces;
using FaceIdBackend.Infrastructure.UnitOfWork;
using FaceIdBackend.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FaceIdBackend.Application.Services;

public class ClassService : IClassService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFlaskFaceRecognitionService _flaskFaceService;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<ClassService> _logger;

    public ClassService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFlaskFaceRecognitionService flaskFaceService,
        IFileStorageService fileStorage,
        ILogger<ClassService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _flaskFaceService = flaskFaceService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<List<ClassDto>> GetAllClassesAsync()
    {
        var classes = await _unitOfWork.Classes
            .GetQueryable()
            .Where(c => c.IsActive)
            .OrderBy(c => c.ClassName)
            .ToListAsync();

        return _mapper.Map<List<ClassDto>>(classes);
    }

    public async Task<ClassDto?> GetClassByIdAsync(Guid id)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(id);
        return classEntity != null ? _mapper.Map<ClassDto>(classEntity) : null;
    }

    public async Task<ClassDto> CreateClassAsync(CreateClassRequest request)
    {
        // Check if class code already exists
        var existingClass = await _unitOfWork.Classes
            .FirstOrDefaultAsync(c => c.ClassCode == request.ClassCode);

        if (existingClass != null)
            throw new InvalidOperationException($"Class with code {request.ClassCode} already exists");

        var classEntity = new Class
        {
            ClassId = Guid.NewGuid(),
            ClassName = request.ClassName,
            ClassCode = request.ClassCode,
            Description = request.Description,
            Location = request.Location,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Flask API doesn't require pre-creating class database
        // It will be created when students are enrolled
        await _unitOfWork.Classes.AddAsync(classEntity);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ClassDto>(classEntity);
    }

    public async Task<ClassDto> UpdateClassAsync(Guid id, UpdateClassRequest request)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(id);
        if (classEntity == null)
            throw new KeyNotFoundException($"Class with ID {id} not found");

        // Check if class code is taken by another class
        var existingClass = await _unitOfWork.Classes
            .FirstOrDefaultAsync(c => c.ClassCode == request.ClassCode && c.ClassId != id);

        if (existingClass != null)
            throw new InvalidOperationException($"Class with code {request.ClassCode} already exists");

        classEntity.ClassName = request.ClassName;
        classEntity.ClassCode = request.ClassCode;
        classEntity.Description = request.Description;
        classEntity.Location = request.Location;
        classEntity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Classes.Update(classEntity);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ClassDto>(classEntity);
    }

    public async Task DeleteClassAsync(Guid id)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(id);
        if (classEntity == null)
            throw new KeyNotFoundException($"Class with ID {id} not found");

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Flask API will clean up face_database/{classId}/ folder automatically
            // when no longer referenced

            // Soft delete class
            classEntity.IsActive = false;
            classEntity.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Classes.Update(classEntity);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<List<EnrolledStudentDto>> GetClassStudentsAsync(Guid classId)
    {
        var enrollments = await _unitOfWork.ClassEnrollments
            .GetQueryable()
            .Include(e => e.Student)
            .Where(e => e.ClassId == classId && e.Status == "Active" && e.Student.IsActive)
            .OrderBy(e => e.Student.StudentNumber)
            .ToListAsync();

        return enrollments.Select(e => new EnrolledStudentDto
        {
            StudentId = e.Student.StudentId,
            StudentNumber = e.Student.StudentNumber,
            FirstName = e.Student.FirstName,
            LastName = e.Student.LastName,
            Email = e.Student.Email,
            ProfilePhotoUrl = e.Student.ProfilePhotoUrl,
            EnrolledAt = e.EnrolledAt,
            Status = e.Status
        }).ToList();
    }

    public async Task<ApiResponse<string>> EnrollStudentsAsync(Guid classId, EnrollStudentsRequest request)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null)
            throw new KeyNotFoundException($"Class with ID {classId} not found");

        var successCount = 0;
        var errors = new List<string>();
        var studentImages = new Dictionary<Guid, IFormFile>();

        foreach (var studentId in request.StudentIds)
        {
            try
            {
                var student = await _unitOfWork.Students.GetByIdAsync(studentId);
                if (student == null)
                {
                    errors.Add($"Student {studentId} not found");
                    continue;
                }

                // Check if already enrolled
                var existingEnrollment = await _unitOfWork.ClassEnrollments
                    .FirstOrDefaultAsync(e => e.ClassId == classId && e.StudentId == studentId);

                if (existingEnrollment != null)
                {
                    if (existingEnrollment.Status == "Active")
                    {
                        errors.Add($"Student {student.StudentNumber} is already enrolled");
                        continue;
                    }
                    else
                    {
                        // Reactivate enrollment
                        existingEnrollment.Status = "Active";
                        existingEnrollment.EnrolledAt = DateTime.UtcNow;
                        _unitOfWork.ClassEnrollments.Update(existingEnrollment);
                    }
                }
                else
                {
                    // Create new enrollment
                    var enrollment = new ClassEnrollment
                    {
                        EnrollmentId = Guid.NewGuid(),
                        ClassId = classId,
                        StudentId = studentId,
                        Status = "Active",
                        EnrolledAt = DateTime.UtcNow
                    };
                    await _unitOfWork.ClassEnrollments.AddAsync(enrollment);
                }

                // Collect student image for Flask API batch setup
                _logger.LogInformation($"Student {student.StudentNumber} ProfilePhotoUrl: {student.ProfilePhotoUrl ?? "NULL"}");

                if (!string.IsNullOrWhiteSpace(student.ProfilePhotoUrl))
                {
                    try
                    {
                        // Convert file path to IFormFile for Flask API
                        var fileInfo = new FileInfo(student.ProfilePhotoUrl);
                        _logger.LogInformation($"File exists: {fileInfo.Exists}, Path: {student.ProfilePhotoUrl}");

                        if (fileInfo.Exists)
                        {
                            var fileStream = new FileStream(student.ProfilePhotoUrl, FileMode.Open, FileAccess.Read);
                            var formFile = new FormFile(fileStream, 0, fileStream.Length, "student_" + studentId, fileInfo.Name)
                            {
                                Headers = new HeaderDictionary(),
                                ContentType = "image/jpeg"
                            };
                            studentImages[studentId] = formFile;
                            _logger.LogInformation($"Added student {studentId} to Flask setup batch");
                        }
                        else
                        {
                            _logger.LogWarning($"Photo file not found for student {student.StudentNumber}: {student.ProfilePhotoUrl}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to load photo for student {student.StudentNumber}");
                        errors.Add($"Failed to load photo for student {student.StudentNumber}: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Student {student.StudentNumber} has no profile photo");
                }

                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Error enrolling student {studentId}: {ex.Message}");
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Setup Flask Face API class database with all enrolled students
        _logger.LogInformation($"Collected {studentImages.Count} student images for Flask setup");

        if (studentImages.Any())
        {
            try
            {
                _logger.LogInformation($"Calling Flask API to setup class database for {classId} with {studentImages.Count} students");
                var flaskResult = await _flaskFaceService.SetupClassDatabaseAsync(classId, studentImages);
                if (!flaskResult.Success)
                {
                    _logger.LogWarning($"Flask setup failed: {flaskResult.Error}");
                    errors.Add($"Warning: Face database setup failed: {flaskResult.Error}");
                }
                else
                {
                    _logger.LogInformation($"Flask setup successful for class {classId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Flask setup error for class {classId}");
                errors.Add($"Warning: Face database setup error: {ex.Message}");
            }
            finally
            {
                // Cleanup file streams
                foreach (var (_, formFile) in studentImages)
                {
                    if (formFile is FormFile ff)
                    {
                        await ff.OpenReadStream().DisposeAsync();
                    }
                }
            }
        }

        var message = $"{successCount} student(s) enrolled successfully";
        if (errors.Any())
        {
            message += $". {errors.Count} error(s) occurred.";
        }

        return new ApiResponse<string>
        {
            Success = successCount > 0,
            Message = message,
            Data = $"{successCount}/{request.StudentIds.Count}",
            Errors = errors.Any() ? errors : null
        };
    }

    public async Task RemoveStudentFromClassAsync(Guid classId, Guid studentId)
    {
        var enrollment = await _unitOfWork.ClassEnrollments
            .FirstOrDefaultAsync(e => e.ClassId == classId && e.StudentId == studentId);

        if (enrollment == null)
            throw new KeyNotFoundException("Enrollment not found");

        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);

        if (classEntity == null || student == null)
            throw new KeyNotFoundException("Class or Student not found");

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Remove from Flask Face API class database
            await _flaskFaceService.DeleteStudentFromClassAsync(classId, studentId);

            // Remove enrollment
            _unitOfWork.ClassEnrollments.Remove(enrollment);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
