using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;

namespace Skillforge.Repository;

public class AttendanceRequestRepository : IAttendanceRequestRepository
{
    private readonly SkillForgeDB _context;

    public AttendanceRequestRepository(SkillForgeDB context)
    {
        _context = context;
    }

    public async Task<AttendanceRequest> CreateAsync(AttendanceRequest request)
    {
        _context.AttendanceRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<List<AttendanceRequest>> GetByEmployeeAsync(int employeeId)
    {
        return await _context.AttendanceRequests
            .Include(r => r.EnrollmentIdNavigation)
                .ThenInclude(e => e.CourseIdNavigation)
            .Include(r => r.EnrollmentIdNavigation)
                .ThenInclude(e => e.EmployeeIdNavigation)
            .Where(r => r.EnrollmentIdNavigation.EmployeeID == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<AttendanceRequest>> GetPendingByCourseAsync(int courseId)
    {
        return await _context.AttendanceRequests
            .Include(r => r.EnrollmentIdNavigation)
                .ThenInclude(e => e.EmployeeIdNavigation)
            .Include(r => r.EnrollmentIdNavigation)
                .ThenInclude(e => e.CourseIdNavigation)
            .Where(r =>
                r.EnrollmentIdNavigation.CourseID == courseId &&
                r.Status == AttendanceRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<AttendanceRequest?> GetByIdAsync(int requestId)
    {
        return await _context.AttendanceRequests
            .Include(r => r.EnrollmentIdNavigation)
                .ThenInclude(e => e.CourseIdNavigation)
            .Include(r => r.EnrollmentIdNavigation)
                .ThenInclude(e => e.EmployeeIdNavigation)
            .FirstOrDefaultAsync(r => r.RequestID == requestId);
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}
