using System;
using System.Collections.Generic;

namespace FaceIdBackend.Domain.Data;

public partial class AttendanceRecord
{
    public Guid AttendanceId { get; set; }

    public Guid SessionId { get; set; }

    public Guid StudentId { get; set; }

    public DateTime CheckInTime { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public string Status { get; set; } = null!;

    public bool IsManualOverride { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AttendanceSession Session { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
