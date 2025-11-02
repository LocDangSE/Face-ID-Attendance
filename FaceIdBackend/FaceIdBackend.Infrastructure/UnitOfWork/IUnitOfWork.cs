using FaceIdBackend.Domain.Data;
using FaceIdBackend.Infrastructure.Repositories;

namespace FaceIdBackend.Infrastructure.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IRepository<Student> Students { get; }
    IRepository<Class> Classes { get; }
    IRepository<ClassEnrollment> ClassEnrollments { get; }
    IRepository<AttendanceSession> AttendanceSessions { get; }
    IRepository<AttendanceRecord> AttendanceRecords { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
