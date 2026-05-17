using System;
using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;

namespace Skillforge.Repository;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly SkillForgeDB context;

    public EnrollmentRepository(SkillForgeDB context)
    {
        this.context = context;
    }
    public async Task AddAsync(Enrollment enrollment)
    {

        await context.Enrollments.AddAsync(enrollment);
        await  context.SaveChangesAsync();

    }

    public async Task AddAuditLog(AuditLog auditLog)
    {
        await context.AuditLogs.AddAsync(auditLog);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int courseId, int employeeId)
    {

        return await context.Enrollments.AnyAsync(e => e.CourseID == courseId && e.EmployeeID == employeeId);

    }

    public async Task<Course> GetByIdAsync(int courseId)
    {
        return await context.Courses.FindAsync(courseId);
    }

    // Checks if an employee exists and is active - used by bulk enrollment to validate each employee
    public async Task<bool> EmployeeExistsAsync(int employeeId)
    {
        return await context.Users.AnyAsync(u => u.UserID == employeeId && u.Status == true);
    }

    public async Task<User?> GetEmployeeByIdAsync(int employeeId)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.UserID == employeeId);
    }

    public async Task<List<Enrollment>> GetAllEnrollmentsAsync()
    {
        return await context.Enrollments
            .Include(e => e.EmployeeIdNavigation)
            .Include(e => e.CourseIdNavigation)
            .Include(e => e.Attendances)
            .OrderByDescending(e => e.EnrollmentDate)
            .ToListAsync();
    }

    public async Task<List<Enrollment>> GetEnrollmentsByEmployeeAsync(int employeeId)
    {
		return await context.Enrollments
	    .Include(e => e.EmployeeIdNavigation)
	    .Include(e => e.CourseIdNavigation)
	    .Include(e => e.Attendances)
	    .Where(e => e.EmployeeID == employeeId)
	    .Where(e => e.CourseIdNavigation.Status == true)  // only live courses
	    .OrderByDescending(e => e.EnrollmentDate)
	    .ToListAsync();
	}

    public async Task<Enrollment?> GetByEnrollmentIdAsync(int enrollmentId)
    {
        return await context.Enrollments.FindAsync(enrollmentId);
    }

    public async Task UpdateEnrollmentStatusAsync(int enrollmentId, bool status)
    {
        var enrollment = await context.Enrollments.FindAsync(enrollmentId);
        if (enrollment != null)
        {
            enrollment.Status = status;
            await context.SaveChangesAsync();
        }
    }
	public async Task<List<Enrollment>> GetEnrollmentsByTrainerAsync(int trainerId)
	{
		return await context.Enrollments
			.Include(e => e.EmployeeIdNavigation)
			.Include(e => e.CourseIdNavigation)
			.Include(e => e.Attendances)
			.Where(e => e.CourseIdNavigation.TrainerID == trainerId)
			.OrderByDescending(e => e.EnrollmentDate)
			.ToListAsync();
	}
}
