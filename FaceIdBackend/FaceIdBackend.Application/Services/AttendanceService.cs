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
using OfficeOpenXml;
using Microsoft.Extensions.Logging;

namespace FaceIdBackend.Application.Services;

/// <summary>
/// Refactored AttendanceService using FlaskApiClient and SessionService
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFlaskApiClient _flaskApiClient;
    private readonly ISessionService _sessionService;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFlaskApiClient flaskApiClient,
        ISessionService sessionService,
        ILogger<AttendanceService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _flaskApiClient = flaskApiClient;
        _sessionService = sessionService;
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

        // Use SessionService to create session with Supabase folder structure
        var session = await _sessionService.CreateSessionAsync(
            request.ClassId,
            request.SessionDate,
            request.Location);

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
        // Use SessionService to complete session
        await _sessionService.CompleteSessionAsync(sessionId);

        // Calculate statistics
        var details = await GetSessionDetailsAsync(sessionId);

        return new CompleteSessionResponse
        {
            SessionId = sessionId,
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

    /// <summary>
    /// Recognize face and mark attendance
    /// 
    /// ANTI-LOOP PROTECTION:
    /// - Loads existing attendance records for the session
    /// - Checks if student already has attendance marked BEFORE calling Flask
    /// - Skips students who are already marked present
    /// - Returns appropriate status for already-attended students
    /// </summary>
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

        // ANTI-LOOP: Pre-load existing attendance records to check for duplicates
        var existingAttendance = await _unitOfWork.AttendanceRecords
            .GetQueryable()
            .Where(a => a.SessionId == sessionId && a.Status == "Present")
            .Select(a => a.StudentId)
            .ToListAsync();

        _logger.LogInformation("Session {SessionId} already has {Count} students marked present",
            sessionId, existingAttendance.Count);

        _logger.LogInformation("Calling Flask API for face recognition. ClassId: {ClassId}", session.ClassId);

        var recognizedStudents = new List<RecognizedStudentDto>();

        try
        {
            // Use FlaskApiClient to recognize faces — pass both sessionId and classId so Flask can track per-session attendance
            var flaskResult = await _flaskApiClient.AnalyzeFacesAsync(session.SessionId, session.ClassId, image);
            _logger.LogInformation("Flask API response - Success: {Success}, Message: {Message}",
                flaskResult.Success, flaskResult.Message);

            if (!flaskResult.Success)
            {
                _logger.LogWarning("Flask recognition failed for Session {SessionId}: {Message}",
                    sessionId, flaskResult.Message);
                return new RecognitionResponse
                {
                    Success = false,
                    Message = flaskResult.Message,
                    RecognizedStudents = new List<RecognizedStudentDto>()
                };
            }

            if (flaskResult.TotalFacesDetected == 0)
            {
                _logger.LogInformation("No faces detected for Session {SessionId}", sessionId);
                return new RecognitionResponse
                {
                    Success = false,
                    Message = "No face detected in image",
                    RecognizedStudents = new List<RecognizedStudentDto>()
                };
            }

            if (!flaskResult.RecognizedStudents.Any())
            {
                _logger.LogInformation("Faces detected but no matches found for Session {SessionId}", sessionId);
                return new RecognitionResponse
                {
                    Success = false,
                    Message = "No students recognized. Faces detected but no matches found.",
                    RecognizedStudents = new List<RecognizedStudentDto>()
                };
            }

            _logger.LogInformation("Flask recognized {Count} candidate(s) for Session {SessionId}",
                flaskResult.RecognizedStudents.Count, sessionId);

            // Process each recognized student
            foreach (var recognizedStudent in flaskResult.RecognizedStudents)
            {
                if (!Guid.TryParse(recognizedStudent.StudentId, out var studentId))
                {
                    _logger.LogWarning("Invalid studentId returned from Flask: {StudentId}",
                        recognizedStudent.StudentId);
                    continue;
                }

                // ANTI-LOOP: Check if student already marked present in this session
                if (existingAttendance.Contains(studentId))
                {
                    _logger.LogInformation(
                        "⚠️ ANTI-LOOP: Student {StudentId} already marked present in Session {SessionId}, skipping",
                        studentId, sessionId);

                    // Add to response with isNewRecord = false to indicate duplicate
                    var existingStudent = await _unitOfWork.Students.GetByIdAsync(studentId);
                    if (existingStudent != null)
                    {
                        recognizedStudents.Add(new RecognizedStudentDto
                        {
                            StudentId = existingStudent.StudentId,
                            StudentNumber = existingStudent.StudentNumber,
                            Name = $"{existingStudent.FirstName} {existingStudent.LastName}",
                            ConfidenceScore = recognizedStudent.Confidence,
                            CheckInTime = TimezoneHelper.GetUtcNowForStorage(),
                            IsNewRecord = false  // Already attended
                        });
                    }
                    continue;  // Skip to next student
                }

                var student = await _unitOfWork.Students.GetByIdAsync(studentId);
                if (student == null)
                {
                    _logger.LogWarning("Student not found in DB: {StudentId}", studentId);
                    continue;
                }

                var enrollment = await _unitOfWork.ClassEnrollments
                    .FirstOrDefaultAsync(e => e.ClassId == session.ClassId &&
                                             e.StudentId == student.StudentId &&
                                             e.Status == "Active");

                if (enrollment == null)
                {
                    _logger.LogInformation("Student {StudentId} is not enrolled in Class {ClassId}",
                        student.StudentId, session.ClassId);
                    continue;
                }

                // This should always be true now due to pre-check above, but keep for safety
                var existingRecord = await _unitOfWork.AttendanceRecords
                    .FirstOrDefaultAsync(a => a.SessionId == sessionId && a.StudentId == student.StudentId);

                bool isNewRecord = existingRecord == null;

                if (isNewRecord)
                {
                    _logger.LogInformation("Creating new attendance record: Session {SessionId}, Student {StudentId}, Confidence {Confidence}",
                        sessionId, student.StudentId, recognizedStudent.Confidence);

                    var attendanceRecord = new AttendanceRecord
                    {
                        AttendanceId = Guid.NewGuid(),
                        SessionId = sessionId,
                        StudentId = student.StudentId,
                        CheckInTime = TimezoneHelper.GetUtcNowForStorage(),
                        ConfidenceScore = recognizedStudent.Confidence,
                        Status = "Present",
                        IsManualOverride = false,
                        CreatedAt = TimezoneHelper.GetUtcNowForStorage()
                    };

                    await _unitOfWork.AttendanceRecords.AddAsync(attendanceRecord);
                    _logger.LogInformation("AttendanceRecord added to context (not yet saved): AttendanceId {AttendanceId}",
                        attendanceRecord.AttendanceId);
                }
                else
                {
                    _logger.LogInformation("Attendance already recorded: Session {SessionId}, Student {StudentId}",
                        sessionId, student.StudentId);
                }

                recognizedStudents.Add(new RecognizedStudentDto
                {
                    StudentId = student.StudentId,
                    StudentNumber = student.StudentNumber,
                    Name = $"{student.FirstName} {student.LastName}",
                    ConfidenceScore = recognizedStudent.Confidence,
                    CheckInTime = TimezoneHelper.GetUtcNowForStorage(),
                    IsNewRecord = isNewRecord
                });
            }

            try
            {
                _logger.LogInformation("Attempting to save {Count} new attendance record(s) to database for Session {SessionId}",
                    recognizedStudents.Count(s => s.IsNewRecord), sessionId);

                var savedCount = await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("✅ Successfully saved {SavedCount} entity change(s) to database for Session {SessionId}",
                    savedCount, sessionId);

                // Verify records were actually saved by querying back
                var verifyRecords = await _unitOfWork.AttendanceRecords
                    .GetQueryable()
                    .Where(a => a.SessionId == sessionId)
                    .CountAsync();

                _logger.LogInformation("✅ Verification: Session {SessionId} now has {TotalRecords} total attendance record(s) in database",
                    sessionId, verifyRecords);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "❌ Failed to save attendance records for Session {SessionId}. Exception: {ExceptionType}",
                    sessionId, saveEx.GetType().Name);
                _logger.LogError("Exception details: {Message}\nStackTrace: {StackTrace}",
                    saveEx.Message, saveEx.StackTrace);

                if (saveEx.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerMessage}", saveEx.InnerException.Message);
                }

                return new RecognitionResponse
                {
                    Success = false,
                    Message = $"Face recognized but failed to save attendance: {saveEx.Message}",
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
            _logger.LogError(ex, "Error during face recognition for Session {SessionId}", sessionId);
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
                CheckInTime = TimezoneHelper.GetUtcNowForStorage(),
                Status = request.Status,
                IsManualOverride = true,
                Notes = request.Notes,
                CreatedAt = TimezoneHelper.GetUtcNowForStorage()
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
