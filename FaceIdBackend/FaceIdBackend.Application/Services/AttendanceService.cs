using AutoMapper;
using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Domain.Data;
using FaceIdBackend.Infrastructure.Services.Interfaces;
using FaceIdBackend.Infrastructure.UnitOfWork;
using FaceIdBackend.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Microsoft.Extensions.Logging;

namespace FaceIdBackend.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFlaskFaceRecognitionService _flaskFaceService;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFlaskFaceRecognitionService flaskFaceService,
        ILogger<AttendanceService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _flaskFaceService = flaskFaceService;
        _logger = logger;
    }

    public async Task<List<AttendanceSessionDto>> GetAllSessionsAsync()
    {
        var sessions = await _unitOfWork.AttendanceSessions
            .GetQueryable()
            .Include(s => s.Class)
            .OrderByDescending(s => s.SessionDate)
            .ThenByDescending(s => s.SessionStartTime)
            .ToListAsync();

        return _mapper.Map<List<AttendanceSessionDto>>(sessions);
    }

    public async Task<CreateSessionResponse> CreateAttendanceSessionAsync(CreateSessionRequest request)
    {
        var classEntity = await _unitOfWork.Classes
            .GetQueryable()
            .Include(c => c.ClassEnrollments)
                .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(c => c.ClassId == request.ClassId && c.IsActive);

        if (classEntity == null)
            throw new KeyNotFoundException($"Class with ID {request.ClassId} not found");

        // Check if there's already an active session for this class today
        var existingSession = await _unitOfWork.AttendanceSessions
            .FirstOrDefaultAsync(s => s.ClassId == request.ClassId &&
                                     s.SessionDate == request.SessionDate &&
                                     s.Status == "InProgress");

        if (existingSession != null)
            throw new InvalidOperationException($"An active session already exists for this class on {request.SessionDate}");

        var session = new AttendanceSession
        {
            SessionId = Guid.NewGuid(),
            ClassId = request.ClassId,
            SessionDate = request.SessionDate,
            SessionStartTime = DateTime.UtcNow,
            Status = "InProgress",
            Location = request.Location,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AttendanceSessions.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        // Get enrolled students
        var enrolledStudents = classEntity.ClassEnrollments
            .Where(e => e.Status == "Active" && e.Student.IsActive)
            .Select(e => new SessionStudentDto
            {
                StudentId = e.Student.StudentId,
                StudentNumber = e.Student.StudentNumber,
                Name = $"{e.Student.FirstName} {e.Student.LastName}",
                Status = "Absent"
            })
            .ToList();

        return new CreateSessionResponse
        {
            SessionId = session.SessionId,
            ClassId = session.ClassId,
            ClassName = classEntity.ClassName,
            SessionDate = session.SessionDate,
            SessionStartTime = session.SessionStartTime,
            Status = session.Status,
            AzurePersonGroupId = classEntity.AzurePersonGroupId,
            EnrolledStudents = enrolledStudents
        };
    }

    public async Task<SessionDetailsDto> GetSessionDetailsAsync(Guid sessionId)
    {
        var session = await _unitOfWork.AttendanceSessions
            .GetQueryable()
            .Include(s => s.Class)
                .ThenInclude(c => c.ClassEnrollments)
                    .ThenInclude(e => e.Student)
            .Include(s => s.AttendanceRecords)
                .ThenInclude(a => a.Student)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            throw new KeyNotFoundException($"Session with ID {sessionId} not found");

        var enrolledStudents = session.Class.ClassEnrollments
            .Where(e => e.Status == "Active" && e.Student.IsActive)
            .ToList();

        var students = enrolledStudents.Select(e =>
        {
            var attendanceRecord = session.AttendanceRecords
                .FirstOrDefault(a => a.StudentId == e.StudentId);

            return new AttendanceDetailDto
            {
                StudentId = e.Student.StudentId,
                StudentNumber = e.Student.StudentNumber,
                Name = $"{e.Student.FirstName} {e.Student.LastName}",
                Status = attendanceRecord?.Status ?? "Absent",
                CheckInTime = attendanceRecord?.CheckInTime,
                ConfidenceScore = attendanceRecord?.ConfidenceScore,
                IsManualOverride = attendanceRecord?.IsManualOverride ?? false
            };
        }).ToList();

        var presentCount = students.Count(s => s.Status == "Present");
        var totalEnrolled = students.Count;
        var attendanceRate = totalEnrolled > 0 ? (double)presentCount / totalEnrolled * 100 : 0;

        return new SessionDetailsDto
        {
            SessionId = session.SessionId,
            ClassName = session.Class.ClassName,
            SessionDate = session.SessionDate,
            SessionStartTime = session.SessionStartTime,
            SessionEndTime = session.SessionEndTime,
            Status = session.Status,
            Location = session.Location,
            TotalEnrolled = totalEnrolled,
            PresentCount = presentCount,
            AbsentCount = totalEnrolled - presentCount,
            AttendanceRate = Math.Round(attendanceRate, 2),
            Students = students
        };
    }

    public async Task<CompleteSessionResponse> CompleteSessionAsync(Guid sessionId)
    {
        var session = await _unitOfWork.AttendanceSessions.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {sessionId} not found");

        if (session.Status != "InProgress")
            throw new InvalidOperationException("Session is not in progress");

        session.SessionEndTime = DateTime.UtcNow;
        session.Status = "Completed";

        _unitOfWork.AttendanceSessions.Update(session);
        await _unitOfWork.SaveChangesAsync();

        // Calculate statistics
        var details = await GetSessionDetailsAsync(sessionId);

        return new CompleteSessionResponse
        {
            SessionId = session.SessionId,
            TotalStudents = details.TotalEnrolled,
            PresentCount = details.PresentCount,
            AbsentCount = details.AbsentCount,
            AttendanceRate = details.AttendanceRate
        };
    }

    public async Task<List<AttendanceSessionDto>> GetActiveSessionsAsync()
    {
        var sessions = await _unitOfWork.AttendanceSessions
            .GetQueryable()
            .Include(s => s.Class)
                .ThenInclude(c => c.ClassEnrollments)
            .Include(s => s.AttendanceRecords)
            .Where(s => s.Status == "InProgress")
            .OrderByDescending(s => s.SessionStartTime)
            .ToListAsync();

        return sessions.Select(s =>
        {
            var totalEnrolled = s.Class.ClassEnrollments.Count(e => e.Status == "Active");
            var presentCount = s.AttendanceRecords.Count(a => a.Status == "Present");

            return new AttendanceSessionDto
            {
                SessionId = s.SessionId,
                ClassId = s.ClassId,
                ClassName = s.Class.ClassName,
                SessionDate = s.SessionDate,
                SessionStartTime = s.SessionStartTime,
                SessionEndTime = s.SessionEndTime,
                Status = s.Status,
                Location = s.Location,
                TotalEnrolled = totalEnrolled,
                PresentCount = presentCount,
                AbsentCount = totalEnrolled - presentCount
            };
        }).ToList();
    }

    public async Task<RecognitionResponse> RecognizeFaceAndMarkAttendanceAsync(Guid sessionId, IFormFile image)
    {
        _logger.LogInformation("RecognizeFaceAndMarkAttendanceAsync called for SessionId: {SessionId}", sessionId);

        var session = await _unitOfWork.AttendanceSessions
            .GetQueryable()
            .Include(s => s.Class)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            throw new KeyNotFoundException($"Session with ID {sessionId} not found");
        }

        if (session.Status != "InProgress")
        {
            _logger.LogWarning("Session {SessionId} is not in progress. Status: {Status}", sessionId, session.Status);
            throw new InvalidOperationException("Session is not in progress");
        }

        _logger.LogInformation("Calling Flask API for face recognition. ClassId: {ClassId}", session.ClassId);

        var recognizedStudents = new List<RecognizedStudentDto>();

        try
        {
            // Call Flask API to recognize faces
            var flaskResult = await _flaskFaceService.RecognizeFaceAsync(session.ClassId, image);
            _logger.LogInformation("Flask API response - Success: {Success}, Message: {Message}", flaskResult.Success, flaskResult.Message);

            if (!flaskResult.Success || flaskResult.Data == null)
            {
                _logger.LogWarning("Flask recognition failed for Session {SessionId}: {Message}", sessionId, flaskResult.Message);
                return new RecognitionResponse
                {
                    Success = false,
                    Message = flaskResult.Message ?? "Face recognition failed",
                    RecognizedStudents = new List<RecognizedStudentDto>()
                };
            }

            if (flaskResult.Data.TotalFacesDetected == 0)
            {
                _logger.LogInformation("No faces detected for Session {SessionId}", sessionId);
                return new RecognitionResponse
                {
                    Success = false,
                    Message = "No face detected in image",
                    RecognizedStudents = new List<RecognizedStudentDto>()
                };
            }

            if (!flaskResult.Data.RecognizedStudents.Any())
            {
                _logger.LogInformation("Faces detected but no matches found for Session {SessionId}", sessionId);
                return new RecognitionResponse
                {
                    Success = false,
                    Message = "No students recognized. Faces detected but no matches found.",
                    RecognizedStudents = new List<RecognizedStudentDto>()
                };
            }

            _logger.LogInformation("Flask recognized {Count} candidate(s) for Session {SessionId}", flaskResult.Data.RecognizedStudents.Count, sessionId);

            // Process each recognized student
            foreach (var recognizedStudent in flaskResult.Data.RecognizedStudents)
            {
                // Parse student ID from Flask response
                if (!Guid.TryParse(recognizedStudent.StudentId, out var studentId))
                {
                    _logger.LogWarning("Invalid studentId returned from Flask: {StudentId}", recognizedStudent.StudentId);
                    continue;
                }

                // Get student from database
                var student = await _unitOfWork.Students.GetByIdAsync(studentId);
                if (student == null)
                {
                    _logger.LogWarning("Student not found in DB: {StudentId}", studentId);
                    continue;
                }

                // Check if student is enrolled in this class
                var enrollment = await _unitOfWork.ClassEnrollments
                    .FirstOrDefaultAsync(e => e.ClassId == session.ClassId &&
                                             e.StudentId == student.StudentId &&
                                             e.Status == "Active");

                if (enrollment == null)
                {
                    _logger.LogInformation("Student {StudentId} is not enrolled (active) in Class {ClassId}", student.StudentId, session.ClassId);
                    continue;
                }

                // Check if already marked present
                var existingRecord = await _unitOfWork.AttendanceRecords
                    .FirstOrDefaultAsync(a => a.SessionId == sessionId && a.StudentId == student.StudentId);

                bool isNewRecord = existingRecord == null;

                if (isNewRecord)
                {
                    _logger.LogInformation("Creating new attendance record: Session {SessionId}, Student {StudentId}", sessionId, student.StudentId);
                    // Create new attendance record
                    var attendanceRecord = new AttendanceRecord
                    {
                        AttendanceId = Guid.NewGuid(),
                        SessionId = sessionId,
                        StudentId = student.StudentId,
                        CheckInTime = DateTime.UtcNow,
                        ConfidenceScore = recognizedStudent.Confidence,
                        Status = "Present",
                        IsManualOverride = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.AttendanceRecords.AddAsync(attendanceRecord);
                }
                else
                {
                    _logger.LogInformation("Attendance already recorded: Session {SessionId}, Student {StudentId}", sessionId, student.StudentId);
                }

                recognizedStudents.Add(new RecognizedStudentDto
                {
                    StudentId = student.StudentId,
                    StudentNumber = student.StudentNumber,
                    Name = $"{student.FirstName} {student.LastName}",
                    ConfidenceScore = recognizedStudent.Confidence,
                    CheckInTime = DateTime.UtcNow,
                    IsNewRecord = isNewRecord
                });
            }

            // Save changes to database with explicit error handling
            try
            {
                var savedCount = await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Successfully saved {SavedCount} attendance record(s) to database for Session {SessionId}", savedCount, sessionId);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save attendance records to database for Session {SessionId}", sessionId);
                return new RecognitionResponse
                {
                    Success = false,
                    Message = $"Face recognized but failed to save attendance to database: {saveEx.Message}",
                    RecognizedStudents = new List<RecognizedStudentDto>()
                };
            }

            var message = recognizedStudents.Any()
                ? $"{recognizedStudents.Count} student(s) recognized and marked present"
                : "No students recognized";

            return new RecognitionResponse
            {
                Success = recognizedStudents.Any(),
                Message = message,
                RecognizedStudents = recognizedStudents
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during face recognition and marking attendance for Session {SessionId}", sessionId);
            return new RecognitionResponse
            {
                Success = false,
                Message = $"Error during face recognition: {ex.Message}",
                RecognizedStudents = new List<RecognizedStudentDto>()
            };
        }
    }

    public async Task<ApiResponse<string>> ManualMarkAttendanceAsync(ManualAttendanceRequest request)
    {
        var session = await _unitOfWork.AttendanceSessions.GetByIdAsync(request.SessionId);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {request.SessionId} not found");

        var student = await _unitOfWork.Students.GetByIdAsync(request.StudentId);
        if (student == null)
            throw new KeyNotFoundException($"Student with ID {request.StudentId} not found");

        // Check if student is enrolled in this class
        var enrollment = await _unitOfWork.ClassEnrollments
            .FirstOrDefaultAsync(e => e.ClassId == session.ClassId &&
                                     e.StudentId == request.StudentId &&
                                     e.Status == "Active");

        if (enrollment == null)
            throw new InvalidOperationException("Student is not enrolled in this class");

        // Check for existing record
        var existingRecord = await _unitOfWork.AttendanceRecords
            .FirstOrDefaultAsync(a => a.SessionId == request.SessionId && a.StudentId == request.StudentId);

        if (existingRecord != null)
        {
            // Update existing record
            existingRecord.Status = request.Status;
            existingRecord.IsManualOverride = true;
            existingRecord.Notes = request.Notes;
            _unitOfWork.AttendanceRecords.Update(existingRecord);
        }
        else
        {
            // Create new record
            var attendanceRecord = new AttendanceRecord
            {
                AttendanceId = Guid.NewGuid(),
                SessionId = request.SessionId,
                StudentId = request.StudentId,
                CheckInTime = DateTime.UtcNow,
                Status = request.Status,
                IsManualOverride = true,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AttendanceRecords.AddAsync(attendanceRecord);
        }

        await _unitOfWork.SaveChangesAsync();

        return new ApiResponse<string>
        {
            Success = true,
            Message = "Attendance marked successfully",
            Data = request.Status
        };
    }

    public async Task<AttendanceReportDto> GetAttendanceReportAsync(
        Guid classId, DateOnly dateFrom, DateOnly dateTo, Guid? studentId = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null)
            throw new KeyNotFoundException($"Class with ID {classId} not found");

        // Get all sessions in the date range
        var sessionsQuery = _unitOfWork.AttendanceSessions
            .GetQueryable()
            .Where(s => s.ClassId == classId &&
                       s.SessionDate >= dateFrom &&
                       s.SessionDate <= dateTo)
            .Include(s => s.AttendanceRecords);

        var sessions = await sessionsQuery.ToListAsync();

        // Get enrolled students
        var studentsQuery = _unitOfWork.ClassEnrollments
            .GetQueryable()
            .Include(e => e.Student)
            .Where(e => e.ClassId == classId && e.Status == "Active");

        if (studentId.HasValue)
        {
            studentsQuery = studentsQuery.Where(e => e.StudentId == studentId.Value);
        }

        var enrollments = await studentsQuery.ToListAsync();

        var studentAttendanceList = new List<StudentAttendanceDto>();

        foreach (var enrollment in enrollments)
        {
            var student = enrollment.Student;
            var studentSessions = new List<SessionAttendanceDto>();

            foreach (var session in sessions)
            {
                var attendanceRecord = session.AttendanceRecords
                    .FirstOrDefault(a => a.StudentId == student.StudentId);

                studentSessions.Add(new SessionAttendanceDto
                {
                    SessionDate = session.SessionDate,
                    Status = attendanceRecord?.Status ?? "Absent",
                    CheckInTime = attendanceRecord?.CheckInTime,
                    ConfidenceScore = attendanceRecord?.ConfidenceScore
                });
            }

            var presentCount = studentSessions.Count(s => s.Status == "Present");
            var totalSessions = sessions.Count;
            var attendanceRate = totalSessions > 0 ? (double)presentCount / totalSessions * 100 : 0;

            studentAttendanceList.Add(new StudentAttendanceDto
            {
                StudentId = student.StudentId,
                StudentNumber = student.StudentNumber,
                FirstName = student.FirstName,
                LastName = student.LastName,
                TotalSessions = totalSessions,
                PresentCount = presentCount,
                AbsentCount = totalSessions - presentCount,
                AttendanceRate = Math.Round(attendanceRate, 2),
                Sessions = studentSessions
            });
        }

        var avgAttendanceRate = studentAttendanceList.Any()
            ? studentAttendanceList.Average(s => s.AttendanceRate)
            : 0;

        return new AttendanceReportDto
        {
            ClassName = classEntity.ClassName,
            ClassCode = classEntity.ClassCode,
            DateFrom = dateFrom,
            DateTo = dateTo,
            TotalSessions = sessions.Count,
            AverageAttendanceRate = Math.Round(avgAttendanceRate, 2),
            Students = studentAttendanceList
        };
    }

    public async Task<StudentHistoryDto> GetStudentAttendanceHistoryAsync(Guid studentId, Guid classId)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null)
            throw new KeyNotFoundException($"Student with ID {studentId} not found");

        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null)
            throw new KeyNotFoundException($"Class with ID {classId} not found");

        // Get all sessions for this class
        var sessions = await _unitOfWork.AttendanceSessions
            .GetQueryable()
            .Where(s => s.ClassId == classId)
            .Include(s => s.AttendanceRecords)
            .OrderByDescending(s => s.SessionDate)
            .ToListAsync();

        var sessionsList = sessions.Select(session =>
        {
            var attendanceRecord = session.AttendanceRecords
                .FirstOrDefault(a => a.StudentId == studentId);

            return new SessionAttendanceDto
            {
                SessionDate = session.SessionDate,
                Status = attendanceRecord?.Status ?? "Absent",
                CheckInTime = attendanceRecord?.CheckInTime,
                ConfidenceScore = attendanceRecord?.ConfidenceScore
            };
        }).ToList();

        var presentCount = sessionsList.Count(s => s.Status == "Present");
        var totalSessions = sessions.Count;
        var attendanceRate = totalSessions > 0 ? (double)presentCount / totalSessions * 100 : 0;

        return new StudentHistoryDto
        {
            Student = _mapper.Map<StudentDto>(student),
            Class = _mapper.Map<ClassDto>(classEntity),
            TotalSessions = totalSessions,
            PresentCount = presentCount,
            AbsentCount = totalSessions - presentCount,
            AttendanceRate = Math.Round(attendanceRate, 2),
            Sessions = sessionsList
        };
    }

    public async Task<SessionDetailsDto> GetSessionAttendanceDetailsAsync(Guid sessionId)
    {
        return await GetSessionDetailsAsync(sessionId);
    }

    public async Task<byte[]> ExportAttendanceToExcelAsync(Guid classId, DateOnly dateFrom, DateOnly dateTo)
    {
        var report = await GetAttendanceReportAsync(classId, dateFrom, dateTo);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // Sheet 1: Detailed Attendance
        var detailSheet = package.Workbook.Worksheets.Add("Detailed Attendance");

        // Headers
        detailSheet.Cells[1, 1].Value = "Student Number";
        detailSheet.Cells[1, 2].Value = "First Name";
        detailSheet.Cells[1, 3].Value = "Last Name";
        detailSheet.Cells[1, 4].Value = "Session Date";
        detailSheet.Cells[1, 5].Value = "Status";
        detailSheet.Cells[1, 6].Value = "Check-In Time";
        detailSheet.Cells[1, 7].Value = "Confidence Score";

        int row = 2;
        foreach (var student in report.Students)
        {
            foreach (var session in student.Sessions)
            {
                detailSheet.Cells[row, 1].Value = student.StudentNumber;
                detailSheet.Cells[row, 2].Value = student.FirstName;
                detailSheet.Cells[row, 3].Value = student.LastName;
                detailSheet.Cells[row, 4].Value = session.SessionDate.ToString("yyyy-MM-dd");
                detailSheet.Cells[row, 5].Value = session.Status;
                detailSheet.Cells[row, 6].Value = session.CheckInTime?.ToString("yyyy-MM-dd HH:mm:ss");
                detailSheet.Cells[row, 7].Value = session.ConfidenceScore;
                row++;
            }
        }

        detailSheet.Cells[detailSheet.Dimension.Address].AutoFitColumns();

        // Sheet 2: Summary
        var summarySheet = package.Workbook.Worksheets.Add("Summary");

        summarySheet.Cells[1, 1].Value = "Student Number";
        summarySheet.Cells[1, 2].Value = "First Name";
        summarySheet.Cells[1, 3].Value = "Last Name";
        summarySheet.Cells[1, 4].Value = "Total Sessions";
        summarySheet.Cells[1, 5].Value = "Present";
        summarySheet.Cells[1, 6].Value = "Absent";
        summarySheet.Cells[1, 7].Value = "Attendance Rate (%)";

        row = 2;
        foreach (var student in report.Students)
        {
            summarySheet.Cells[row, 1].Value = student.StudentNumber;
            summarySheet.Cells[row, 2].Value = student.FirstName;
            summarySheet.Cells[row, 3].Value = student.LastName;
            summarySheet.Cells[row, 4].Value = student.TotalSessions;
            summarySheet.Cells[row, 5].Value = student.PresentCount;
            summarySheet.Cells[row, 6].Value = student.AbsentCount;
            summarySheet.Cells[row, 7].Value = student.AttendanceRate;
            row++;
        }

        summarySheet.Cells[summarySheet.Dimension.Address].AutoFitColumns();

        return package.GetAsByteArray();
    }
}
