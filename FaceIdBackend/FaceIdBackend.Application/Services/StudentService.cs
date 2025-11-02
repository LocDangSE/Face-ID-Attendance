using AutoMapper;
using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Domain.Data;
using FaceIdBackend.Infrastructure.Services.Interfaces;
using FaceIdBackend.Infrastructure.UnitOfWork;
using FaceIdBackend.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FaceIdBackend.Application.Services;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorage;
    private readonly IFlaskFaceRecognitionService _flaskFaceService;

    public StudentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileStorageService fileStorage,
        IFlaskFaceRecognitionService flaskFaceService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileStorage = fileStorage;
        _flaskFaceService = flaskFaceService;
    }

    public async Task<List<StudentDto>> GetAllStudentsAsync()
    {
        var students = await _unitOfWork.Students
            .GetQueryable()
            .Where(s => s.IsActive)
            .OrderBy(s => s.StudentNumber)
            .ToListAsync();

        return _mapper.Map<List<StudentDto>>(students);
    }

    public async Task<StudentDto?> GetStudentByIdAsync(Guid id)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id);
        return student != null ? _mapper.Map<StudentDto>(student) : null;
    }

    public async Task<StudentDto> CreateStudentAsync(CreateStudentRequest request, IFormFile photo)
    {
        // Check if student number already exists
        var existingStudent = await _unitOfWork.Students
            .FirstOrDefaultAsync(s => s.StudentNumber == request.StudentNumber);

        if (existingStudent != null)
            throw new InvalidOperationException($"Student with number {request.StudentNumber} already exists");

        var student = new Student
        {
            StudentId = Guid.NewGuid(),
            StudentNumber = request.StudentNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Save photo and register face if provided
        if (photo != null && photo.Length > 0)
        {
            student.ProfilePhotoUrl = await _fileStorage.SaveStudentPhotoAsync(photo, student.StudentId);

            // Register face with Flask API
            var faceResult = await _flaskFaceService.RegisterFaceAsync(student.StudentId, photo);
            if (!faceResult.Success)
            {
                // Log warning but don't fail - can register face later
                // Face registration will happen when student enrolls in a class
            }
        }

        await _unitOfWork.Students.AddAsync(student);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<StudentDto>(student);
    }

    public async Task<StudentDto> UpdateStudentAsync(Guid id, UpdateStudentRequest request)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id);
        if (student == null)
            throw new KeyNotFoundException($"Student with ID {id} not found");

        // Check if student number is taken by another student
        var existingStudent = await _unitOfWork.Students
            .FirstOrDefaultAsync(s => s.StudentNumber == request.StudentNumber && s.StudentId != id);

        if (existingStudent != null)
            throw new InvalidOperationException($"Student with number {request.StudentNumber} already exists");

        student.StudentNumber = request.StudentNumber;
        student.FirstName = request.FirstName;
        student.LastName = request.LastName;
        student.Email = request.Email;
        student.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Students.Update(student);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<StudentDto>(student);
    }

    public async Task DeleteStudentAsync(Guid id)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id);
        if (student == null)
            throw new KeyNotFoundException($"Student with ID {id} not found");

        // Get all classes this student is enrolled in
        var enrollments = await _unitOfWork.ClassEnrollments
            .GetQueryable()
            .Include(e => e.Class)
            .Where(e => e.StudentId == id)
            .ToListAsync();

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Remove from Flask Face API for each class
            foreach (var enrollment in enrollments)
            {
                // Delete student face data from this class
                await _flaskFaceService.DeleteStudentFromClassAsync(enrollment.ClassId, student.StudentId);
            }

            // Delete photo if exists
            if (!string.IsNullOrWhiteSpace(student.ProfilePhotoUrl))
            {
                await _fileStorage.DeleteFileAsync(student.ProfilePhotoUrl);
            }

            // Soft delete student
            student.IsActive = false;
            student.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Students.Update(student);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<StudentDto> UpdateStudentPhotoAsync(Guid id, IFormFile photo)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id);
        if (student == null)
            throw new KeyNotFoundException($"Student with ID {id} not found");

        // Get all classes this student is enrolled in
        var enrollments = await _unitOfWork.ClassEnrollments
            .GetQueryable()
            .Include(e => e.Class)
            .Where(e => e.StudentId == id && e.Status == "Active")
            .ToListAsync();

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Delete old photo if exists
            if (!string.IsNullOrWhiteSpace(student.ProfilePhotoUrl))
            {
                await _fileStorage.DeleteFileAsync(student.ProfilePhotoUrl);
            }

            // Save new photo
            student.ProfilePhotoUrl = await _fileStorage.SaveStudentPhotoAsync(photo, student.StudentId);

            // Re-register face with Flask API
            var faceResult = await _flaskFaceService.RegisterFaceAsync(student.StudentId, photo);
            if (!faceResult.Success)
            {
                // Log warning - will need to re-setup class databases
            }

            student.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Students.Update(student);

            await _unitOfWork.CommitTransactionAsync();

            return _mapper.Map<StudentDto>(student);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task DeleteStudentPhotoAsync(Guid id)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id);
        if (student == null)
            throw new KeyNotFoundException($"Student with ID {id} not found");

        // Get all classes this student is enrolled in
        var enrollments = await _unitOfWork.ClassEnrollments
            .GetQueryable()
            .Include(e => e.Class)
            .Where(e => e.StudentId == id)
            .ToListAsync();

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Remove from Flask Face API for each class
            foreach (var enrollment in enrollments)
            {
                await _flaskFaceService.DeleteStudentFromClassAsync(enrollment.ClassId, student.StudentId);
            }

            // Delete photo file
            if (!string.IsNullOrWhiteSpace(student.ProfilePhotoUrl))
            {
                await _fileStorage.DeleteFileAsync(student.ProfilePhotoUrl);
            }

            student.ProfilePhotoUrl = null;
            student.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Students.Update(student);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
