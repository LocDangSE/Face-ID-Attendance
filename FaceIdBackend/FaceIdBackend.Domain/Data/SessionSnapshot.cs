namespace FaceIdBackend.Domain.Data;

/// <summary>
/// Session Snapshot entity - stores complete attendance session history as a "receipt"
/// Captures all recognition results, attendance records, and statistics for audit trail
/// </summary>
public class SessionSnapshot
{
    public Guid SnapshotId { get; set; }
    public Guid SessionId { get; set; }

    // Statistics
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public decimal AttendanceRate { get; set; }

    // Captured Data (stored as JSON)
    public string? CapturedImagesFolder { get; set; }
    public string? RecognitionResultsJson { get; set; }
    public string? AttendanceRecordsJson { get; set; }
    public string? SessionMetadataJson { get; set; }

    // Metadata
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedBy { get; set; }
    public DateTime? SessionStartTime { get; set; }
    public DateTime? SessionEndTime { get; set; }
    public TimeSpan? SessionDuration { get; set; }

    // Navigation properties
    public virtual AttendanceSession Session { get; set; } = null!;
}
