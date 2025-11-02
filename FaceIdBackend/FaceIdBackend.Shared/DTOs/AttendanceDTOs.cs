using Microsoft.AspNetCore.Http;

namespace FaceIdBackend.Shared.DTOs;

// Student DTOs
public class StudentDto
{
    public Guid StudentId { get; set; }
    public string StudentNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? ProfilePhotoUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateStudentRequest
{
    public string StudentNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public IFormFile Photo { get; set; } = null!;
}

public class UpdateStudentRequest
{
    public string StudentNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
}

// Class DTOs
public class ClassDto
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public string ClassCode { get; set; } = null!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateClassRequest
{
    public string ClassName { get; set; } = null!;
    public string ClassCode { get; set; } = null!;
    public string? Description { get; set; }
    public string? Location { get; set; }
}

public class UpdateClassRequest
{
    public string ClassName { get; set; } = null!;
    public string ClassCode { get; set; } = null!;
    public string? Description { get; set; }
    public string? Location { get; set; }
}

public class EnrollStudentsRequest
{
    public List<Guid> StudentIds { get; set; } = new();
}

public class EnrolledStudentDto
{
    public Guid StudentId { get; set; }
    public string StudentNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? ProfilePhotoUrl { get; set; }
    public DateTime EnrolledAt { get; set; }
    public string Status { get; set; } = null!;
}

// Attendance Session DTOs
public class AttendanceSessionDto
{
    public Guid SessionId { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public DateOnly SessionDate { get; set; }
    public DateTime SessionStartTime { get; set; }
    public DateTime? SessionEndTime { get; set; }
    public string Status { get; set; } = null!;
    public string? Location { get; set; }
    public int TotalEnrolled { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }

    // Nested class object for frontend compatibility
    public ClassInfo? Class { get; set; }
}

public class ClassInfo
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public string ClassCode { get; set; } = null!;
}

public class CreateSessionRequest
{
    public Guid ClassId { get; set; }
    public DateOnly SessionDate { get; set; }
    public string? Location { get; set; }
}

public class CreateSessionResponse
{
    public Guid SessionId { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public DateOnly SessionDate { get; set; }
    public DateTime SessionStartTime { get; set; }
    public string Status { get; set; } = null!;
    public string? AzurePersonGroupId { get; set; }
    public List<SessionStudentDto> EnrolledStudents { get; set; } = new();
}

public class SessionStudentDto
{
    public Guid StudentId { get; set; }
    public string StudentNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Status { get; set; } = "Absent";
}

public class SessionDetailsDto
{
    public Guid SessionId { get; set; }
    public string ClassName { get; set; } = null!;
    public DateOnly SessionDate { get; set; }
    public DateTime SessionStartTime { get; set; }
    public DateTime? SessionEndTime { get; set; }
    public string Status { get; set; } = null!;
    public string? Location { get; set; }
    public int TotalEnrolled { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public double AttendanceRate { get; set; }
    public List<AttendanceDetailDto> Students { get; set; } = new();

    // Nested class object for frontend compatibility
    public ClassInfo? Class { get; set; }
}

public class AttendanceDetailDto
{
    public Guid StudentId { get; set; }
    public string StudentNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? CheckInTime { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public bool IsManualOverride { get; set; }
}

public class CompleteSessionResponse
{
    public Guid SessionId { get; set; }
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public double AttendanceRate { get; set; }
}

// Face Recognition DTOs
public class RecognizeStudentRequest
{
    public Guid SessionId { get; set; }
    public IFormFile Image { get; set; } = null!;
}

public class RecognitionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public List<RecognizedStudentDto> RecognizedStudents { get; set; } = new();
}

public class RecognizedStudentDto
{
    public Guid StudentId { get; set; }
    public string StudentNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal ConfidenceScore { get; set; }
    public DateTime CheckInTime { get; set; }
    public bool IsNewRecord { get; set; }
}

public class ManualAttendanceRequest
{
    public Guid SessionId { get; set; }
    public Guid StudentId { get; set; }
    public string Status { get; set; } = "Present";
    public string? Notes { get; set; }
}

// Attendance Report DTOs
public class AttendanceReportRequest
{
    public Guid ClassId { get; set; }
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public Guid? StudentId { get; set; }
}

public class AttendanceReportDto
{
    public string ClassName { get; set; } = null!;
    public string ClassCode { get; set; } = null!;
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public int TotalSessions { get; set; }
    public double AverageAttendanceRate { get; set; }
    public List<StudentAttendanceDto> Students { get; set; } = new();
}

public class StudentAttendanceDto
{
    public Guid StudentId { get; set; }
    public string StudentNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public int TotalSessions { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public double AttendanceRate { get; set; }
    public List<SessionAttendanceDto> Sessions { get; set; } = new();
}

public class SessionAttendanceDto
{
    public DateOnly SessionDate { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? CheckInTime { get; set; }
    public decimal? ConfidenceScore { get; set; }
}

public class StudentHistoryDto
{
    public StudentDto Student { get; set; } = null!;
    public ClassDto Class { get; set; } = null!;
    public int TotalSessions { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public double AttendanceRate { get; set; }
    public List<SessionAttendanceDto> Sessions { get; set; } = new();
}

// Dashboard DTOs
public class DashboardStatsDto
{
    public int TotalStudents { get; set; }
    public int TotalClasses { get; set; }
    public int ActiveSessions { get; set; }
    public int TodaySessions { get; set; }
    public List<RecentSessionDto> RecentSessions { get; set; } = new();
}

public class RecentSessionDto
{
    public Guid SessionId { get; set; }
    public string ClassName { get; set; } = null!;
    public DateOnly SessionDate { get; set; }
    public double AttendanceRate { get; set; }
}

public class ClassStatisticsDto
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public int TotalStudents { get; set; }
    public int TotalSessions { get; set; }
    public double AverageAttendanceRate { get; set; }
    public List<AttendanceTrendDto> AttendanceTrend { get; set; } = new();
}

public class AttendanceTrendDto
{
    public DateOnly Date { get; set; }
    public double Rate { get; set; }
}

// Generic Response
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
}
