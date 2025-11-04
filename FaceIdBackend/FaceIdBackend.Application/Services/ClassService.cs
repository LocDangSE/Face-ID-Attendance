using AutoMapper;
using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Domain.Data;
using FaceIdBackend.Infrastructure.Services;
using FaceIdBackend.Infrastructure.Services.Interfaces;
using FaceIdBackend.Infrastructure.UnitOfWork;
using FaceIdBackend.Shared.DTOs;
using FaceIdBackend.Shared.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FaceIdBackend.Application.Services;

public class ClassService : IClassService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFlaskFaceRecognitionService _flaskFaceService;
    private readonly IFlaskApiClient _flaskApiClient;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<ClassService> _logger;

    public ClassService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFlaskFaceRecognitionService flaskFaceService,
        IFlaskApiClient flaskApiClient,
        IFileStorageService fileStorage,
        ILogger<ClassService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _flaskFaceService = flaskFaceService;
        _flaskApiClient = flaskApiClient;
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
            CreatedAt = TimezoneHelper.GetUtcNowForStorage()
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
        classEntity.UpdatedAt = TimezoneHelper.GetUtcNowForStorage();

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
            classEntity.UpdatedAt = TimezoneHelper.GetUtcNowForStorage();
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
                        existingEnrollment.EnrolledAt = TimezoneHelper.GetUtcNowForStorage();
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
                        EnrolledAt = TimezoneHelper.GetUtcNowForStorage()
                    };
                    await _unitOfWork.ClassEnrollments.AddAsync(enrollment);
                }

                // Collect student image for Flask API registration
                _logger.LogInformation($"Student {student.StudentNumber} ProfilePhotoUrl: {student.ProfilePhotoUrl ?? "NULL"}");

                if (!string.IsNullOrWhiteSpace(student.ProfilePhotoUrl))
                {
                    try
                    {
                        // Check if URL or local path
                        if (student.ProfilePhotoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            student.ProfilePhotoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            // Download from URL (Supabase)
                            using var httpClient = new HttpClient();
                            var photoBytes = await httpClient.GetByteArrayAsync(student.ProfilePhotoUrl);
                            var photoStream = new MemoryStream(photoBytes);

                            var formFile = new FormFile(photoStream, 0, photoBytes.Length, "student_" + studentId, $"{studentId}.jpg")
                            {
                                Headers = new HeaderDictionary(),
                                ContentType = "image/jpeg"
                            };
                            studentImages[studentId] = formFile;
                            _logger.LogInformation($"✅ Downloaded photo for student {student.StudentNumber} from Supabase");
                        }
                        else
                        {
                            // Local file path
                            var fileInfo = new FileInfo(student.ProfilePhotoUrl);
                            if (fileInfo.Exists)
                            {
                                var fileStream = new FileStream(student.ProfilePhotoUrl, FileMode.Open, FileAccess.Read);
                                var formFile = new FormFile(fileStream, 0, fileStream.Length, "student_" + studentId, fileInfo.Name)
                                {
                                    Headers = new HeaderDictionary(),
                                    ContentType = "image/jpeg"
                                };
                                studentImages[studentId] = formFile;
                                _logger.LogInformation($"✅ Loaded local photo for student {student.StudentNumber}");
                            }
                            else
                            {
                                _logger.LogWarning($"❌ Photo file not found for student {student.StudentNumber}: {student.ProfilePhotoUrl}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"❌ Failed to load photo for student {student.StudentNumber}");
                        errors.Add($"Failed to load photo for student {student.StudentNumber}: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogWarning($"❌ Student {student.StudentNumber} has no profile photo");
                }

                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Error enrolling student {studentId}: {ex.Message}");
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Register students with Flask Face API individually
        _logger.LogInformation($"Collected {studentImages.Count} student images for Flask registration");

        if (studentImages.Any())
        {
            var registeredCount = 0;
            var registrationErrors = new List<string>();

            foreach (var (studentId, imageFile) in studentImages)
            {
                try
                {
                    _logger.LogInformation($"Registering student {studentId} with Flask API");

                    // Call Flask register endpoint for each student
                    var flaskResult = await _flaskApiClient.RegisterStudentAsync(studentId, imageFile);

                    if (flaskResult.Success)
                    {
                        registeredCount++;
                        _logger.LogInformation($"✅ Successfully registered student {studentId} with Flask");
                    }
                    else
                    {
                        var errorMsg = $"Student {studentId}: {flaskResult.Message}";
                        registrationErrors.Add(errorMsg);
                        _logger.LogWarning($"❌ Flask registration failed for student {studentId}: {flaskResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Student {studentId}: {ex.Message}";
                    registrationErrors.Add(errorMsg);
                    _logger.LogError(ex, $"❌ Flask registration error for student {studentId}");
                }
            }

            // Cleanup file streams
            foreach (var (_, formFile) in studentImages)
            {
                try
                {
                    if (formFile is FormFile ff)
                    {
                        await ff.OpenReadStream().DisposeAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing file stream");
                }
            }

            _logger.LogInformation($"Flask registration complete: {registeredCount}/{studentImages.Count} students registered");

            if (registrationErrors.Any())
            {
                errors.Add($"Warning: {registrationErrors.Count} student(s) failed Flask registration");
                errors.AddRange(registrationErrors);
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
