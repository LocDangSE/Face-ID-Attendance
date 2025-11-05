using System;
using System.Collections.Generic;
using FaceIdBackend.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace FaceIdBackend.Infrastructure.Context;

public partial class AttendanceSystemContext : DbContext
{
    public AttendanceSystemContext()
    {
    }

    public AttendanceSystemContext(DbContextOptions<AttendanceSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AttendanceRecord> AttendanceRecords { get; set; }

    public virtual DbSet<AttendanceSession> AttendanceSessions { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassEnrollment> ClassEnrollments { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<SessionSnapshot> SessionSnapshots { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This will only be used for design-time tools (migrations, etc.)
            // At runtime, the connection string is configured in Program.cs
            optionsBuilder.UseSqlServer("Server=localhost;Database=AttendanceSystemDB;User Id=sa;Password=12345;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69263CAA59BE09");

            entity.HasIndex(e => e.CheckInTime, "IX_Attendance_CheckInTime");

            entity.HasIndex(e => e.SessionId, "IX_Attendance_SessionID");

            entity.HasIndex(e => e.Status, "IX_Attendance_Status");

            entity.HasIndex(e => e.StudentId, "IX_Attendance_StudentID");

            entity.HasIndex(e => new { e.SessionId, e.StudentId }, "UQ_Session_Student").IsUnique();

            entity.Property(e => e.AttendanceId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("AttendanceID");
            entity.Property(e => e.CheckInTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ConfidenceScore).HasColumnType("decimal(5, 4)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.SessionId).HasColumnName("SessionID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Present");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");

            entity.HasOne(d => d.Session).WithMany(p => p.AttendanceRecords)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_Attendance_Session");

            entity.HasOne(d => d.Student).WithMany(p => p.AttendanceRecords)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_Attendance_Student");
        });

        modelBuilder.Entity<AttendanceSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__Attendan__C9F49270DC14CDA5");

            entity.HasIndex(e => e.ClassId, "IX_Sessions_ClassID");

            entity.HasIndex(e => new { e.ClassId, e.SessionDate }, "IX_Sessions_Class_Date");

            entity.HasIndex(e => e.SessionDate, "IX_Sessions_Date");

            entity.HasIndex(e => e.Status, "IX_Sessions_Status");

            entity.Property(e => e.SessionId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("SessionID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("InProgress");

            entity.HasOne(d => d.Class).WithMany(p => p.AttendanceSessions)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK_Session_Class");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927A0A783E26D");

            entity.HasIndex(e => e.ClassCode, "IX_Classes_ClassCode");

            entity.HasIndex(e => e.IsActive, "IX_Classes_IsActive");

            entity.HasIndex(e => e.ClassCode, "UQ__Classes__2ECD4A55E3D4F333").IsUnique();

            entity.Property(e => e.ClassId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("ClassID");
            entity.Property(e => e.AzurePersonGroupId).HasMaxLength(255);
            entity.Property(e => e.ClassCode).HasMaxLength(50);
            entity.Property(e => e.ClassName).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Location).HasMaxLength(200);
        });

        modelBuilder.Entity<ClassEnrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__ClassEnr__7F6877FB0FB61386");

            entity.HasIndex(e => e.ClassId, "IX_Enrollments_ClassID");

            entity.HasIndex(e => e.Status, "IX_Enrollments_Status");

            entity.HasIndex(e => e.StudentId, "IX_Enrollments_StudentID");

            entity.HasIndex(e => new { e.ClassId, e.StudentId }, "UQ_Class_Student").IsUnique();

            entity.Property(e => e.EnrollmentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("EnrollmentID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.EnrolledAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassEnrollments)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK_Enrollment_Class");

            entity.HasOne(d => d.Student).WithMany(p => p.ClassEnrollments)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_Enrollment_Student");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Students__32C52A79FE3CA4F8");

            entity.HasIndex(e => e.AzurePersonId, "IX_Students_AzurePersonId");

            entity.HasIndex(e => e.IsActive, "IX_Students_IsActive");

            entity.HasIndex(e => e.StudentNumber, "IX_Students_StudentNumber");

            entity.HasIndex(e => e.StudentNumber, "UQ__Students__DD81BF6C90048E4A").IsUnique();

            entity.Property(e => e.StudentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("StudentID");
            entity.Property(e => e.AzureFaceId).HasMaxLength(255);
            entity.Property(e => e.AzurePersonId).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.ProfilePhotoUrl).HasMaxLength(500);
            entity.Property(e => e.StudentNumber).HasMaxLength(50);
        });

        modelBuilder.Entity<SessionSnapshot>(entity =>
        {
            entity.HasKey(e => e.SnapshotId).HasName("PK__SessionS__SnapshotID");

            entity.HasIndex(e => e.SessionId, "IX_SessionSnapshots_SessionID").IsUnique();
            entity.HasIndex(e => e.GeneratedAt, "IX_SessionSnapshots_GeneratedAt");

            entity.Property(e => e.SnapshotId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("SnapshotID");
            entity.Property(e => e.SessionId).HasColumnName("SessionID");
            entity.Property(e => e.TotalStudents).HasDefaultValue(0);
            entity.Property(e => e.PresentCount).HasDefaultValue(0);
            entity.Property(e => e.AbsentCount).HasDefaultValue(0);
            entity.Property(e => e.LateCount).HasDefaultValue(0);
            entity.Property(e => e.AttendanceRate).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.CapturedImagesFolder).HasMaxLength(500);
            entity.Property(e => e.RecognitionResultsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.AttendanceRecordsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.SessionMetadataJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.GeneratedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.GeneratedBy).HasMaxLength(100);

            entity.HasOne(d => d.Session)
                .WithOne(p => p.SessionSnapshot)
                .HasForeignKey<SessionSnapshot>(d => d.SessionId)
                .HasConstraintName("FK_SessionSnapshot_Session");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
