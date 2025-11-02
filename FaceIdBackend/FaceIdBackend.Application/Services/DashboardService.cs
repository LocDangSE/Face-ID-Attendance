using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Infrastructure.UnitOfWork;
using FaceIdBackend.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FaceIdBackend.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var totalStudents = await _unitOfWork.Students
            .CountAsync(s => s.IsActive);

        var totalClasses = await _unitOfWork.Classes
            .CountAsync(c => c.IsActive);

        var activeSessions = await _unitOfWork.AttendanceSessions
            .CountAsync(s => s.Status == "InProgress");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var todaySessions = await _unitOfWork.AttendanceSessions
            .CountAsync(s => s.SessionDate == today);

        var recentSessions = await _unitOfWork.AttendanceSessions
            .GetQueryable()
            .Include(s => s.Class)
            .Include(s => s.AttendanceRecords)
            .Where(s => s.Class.IsActive)
            .OrderByDescending(s => s.SessionStartTime)
            .Take(5)
            .ToListAsync();

        var recentSessionsList = recentSessions.Select(s =>
        {
            var classEnrollmentCount = _unitOfWork.ClassEnrollments
                .GetQueryable()
                .Count(e => e.ClassId == s.ClassId && e.Status == "Active");

            var presentCount = s.AttendanceRecords.Count(a => a.Status == "Present");
            var rate = classEnrollmentCount > 0
                ? (double)presentCount / classEnrollmentCount * 100
                : 0;

            return new RecentSessionDto
            {
                SessionId = s.SessionId,
                ClassName = s.Class.ClassName,
                SessionDate = s.SessionDate,
                AttendanceRate = Math.Round(rate, 2)
            };
        }).ToList();

        return new DashboardStatsDto
        {
            TotalStudents = totalStudents,
            TotalClasses = totalClasses,
            ActiveSessions = activeSessions,
            TodaySessions = todaySessions,
            RecentSessions = recentSessionsList
        };
    }

    public async Task<ClassStatisticsDto> GetClassStatisticsAsync(Guid classId, DateOnly dateFrom, DateOnly dateTo)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null)
            throw new KeyNotFoundException($"Class with ID {classId} not found");

        var totalStudents = await _unitOfWork.ClassEnrollments
            .CountAsync(e => e.ClassId == classId && e.Status == "Active");

        var sessions = await _unitOfWork.AttendanceSessions
            .GetQueryable()
            .Include(s => s.AttendanceRecords)
            .Where(s => s.ClassId == classId &&
                       s.SessionDate >= dateFrom &&
                       s.SessionDate <= dateTo)
            .OrderBy(s => s.SessionDate)
            .ToListAsync();

        var totalSessions = sessions.Count;

        var attendanceTrend = sessions.Select(s =>
        {
            var presentCount = s.AttendanceRecords.Count(a => a.Status == "Present");
            var rate = totalStudents > 0
                ? (double)presentCount / totalStudents * 100
                : 0;

            return new AttendanceTrendDto
            {
                Date = s.SessionDate,
                Rate = Math.Round(rate, 2)
            };
        }).ToList();

        var averageRate = attendanceTrend.Any()
            ? attendanceTrend.Average(t => t.Rate)
            : 0;

        return new ClassStatisticsDto
        {
            ClassId = classId,
            ClassName = classEntity.ClassName,
            TotalStudents = totalStudents,
            TotalSessions = totalSessions,
            AverageAttendanceRate = Math.Round(averageRate, 2),
            AttendanceTrend = attendanceTrend
        };
    }
}
