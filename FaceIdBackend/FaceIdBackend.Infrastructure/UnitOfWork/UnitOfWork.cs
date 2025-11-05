using FaceIdBackend.Domain.Data;
using FaceIdBackend.Infrastructure.Context;
using FaceIdBackend.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace FaceIdBackend.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AttendanceSystemContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AttendanceSystemContext context)
    {
        _context = context;
        Students = new Repository<Student>(_context);
        Classes = new Repository<Class>(_context);
        ClassEnrollments = new Repository<ClassEnrollment>(_context);
        AttendanceSessions = new Repository<AttendanceSession>(_context);
        AttendanceRecords = new Repository<AttendanceRecord>(_context);
        SessionSnapshots = new Repository<SessionSnapshot>(_context);
    }

    public IRepository<Student> Students { get; private set; }
    public IRepository<Class> Classes { get; private set; }
    public IRepository<ClassEnrollment> ClassEnrollments { get; private set; }
    public IRepository<AttendanceSession> AttendanceSessions { get; private set; }
    public IRepository<AttendanceRecord> AttendanceRecords { get; private set; }
    public IRepository<SessionSnapshot> SessionSnapshots { get; private set; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
