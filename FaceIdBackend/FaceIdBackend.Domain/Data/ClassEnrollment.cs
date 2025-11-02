using System;
using System.Collections.Generic;

namespace FaceIdBackend.Domain.Data;

public partial class ClassEnrollment
{
    public Guid EnrollmentId { get; set; }

    public Guid ClassId { get; set; }

    public Guid StudentId { get; set; }

    public DateTime EnrolledAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual Class Class { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
