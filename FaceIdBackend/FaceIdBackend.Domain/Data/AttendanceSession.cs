using System;
using System.Collections.Generic;

namespace FaceIdBackend.Domain.Data;

public partial class AttendanceSession
{
    public Guid SessionId { get; set; }

    public Guid ClassId { get; set; }

    public DateOnly SessionDate { get; set; }

    public DateTime SessionStartTime { get; set; }

    public DateTime? SessionEndTime { get; set; }

    public string Status { get; set; } = null!;

    public string? Location { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public virtual Class Class { get; set; } = null!;
}
