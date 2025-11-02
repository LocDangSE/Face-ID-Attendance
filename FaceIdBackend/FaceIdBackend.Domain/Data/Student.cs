using System;
using System.Collections.Generic;

namespace FaceIdBackend.Domain.Data;

public partial class Student
{
    public Guid StudentId { get; set; }

    public string StudentNumber { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? AzurePersonId { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public string? AzureFaceId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public virtual ICollection<ClassEnrollment> ClassEnrollments { get; set; } = new List<ClassEnrollment>();
}
