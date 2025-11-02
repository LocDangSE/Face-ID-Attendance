using System;
using System.Collections.Generic;

namespace FaceIdBackend.Domain.Data;

public partial class Class
{
    public Guid ClassId { get; set; }

    public string ClassName { get; set; } = null!;

    public string ClassCode { get; set; } = null!;

    public string? Description { get; set; }

    public string? AzurePersonGroupId { get; set; }

    public string? Location { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();

    public virtual ICollection<ClassEnrollment> ClassEnrollments { get; set; } = new List<ClassEnrollment>();
}
