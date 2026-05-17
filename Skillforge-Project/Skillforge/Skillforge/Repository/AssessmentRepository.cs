using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;

namespace Skillforge.Repository;

/// <summary>
/// A concrete implementation of IAssessmentRepository that uses Entity Framework Core
/// to handle course lookups and assessment persistence against the SQL Server database.
/// </summary>
public class AssessmentRepository : IAssessmentRepository
{
    private readonly SkillForgeDB _context;

    public AssessmentRepository(SkillForgeDB context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves a course entity from the database using the specified courseId.
    /// Returns null if the course does not exist.
    /// </summary>
    /// <param name="courseId">The unique identifier of the course to retrieve.</param>
    /// <returns>The Course entity if found; otherwise null.</returns>
    public async Task<Course?> GetCourseByIdAsync(int courseId)
    {
        return await _context.Courses.FindAsync(courseId);
    }

    /// <summary>
    /// Inserts a new Assessment record into the database and persists the changes.
    /// </summary>
    /// <param name="assessment">The Assessment entity to insert.</param>
    /// <returns>The auto-generated AssessmentID assigned after the save.</returns>
    public async Task<int> CreateAssessmentAsync(Assessment assessment)
    {
        _context.Assessments.Add(assessment);
        await _context.SaveChangesAsync();
        return assessment.AssessmentID;
    }

    public async Task<List<Assessment>> GetAllAssessmentsAsync()
    {
        return await _context.Assessments
            .Include(a => a.Course)
            .Include(a => a.Module)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    public async Task<List<Assessment>> GetAssessmentsForEnrolledCoursesAsync(int employeeId)
    {
		var enrolledCourseIds = await _context.Enrollments
	    .Where(e => e.EmployeeID == employeeId)
	    .Where(e => e.CourseIdNavigation.Status == true)  // only live courses
	    .Select(e => e.CourseID)
	    .ToListAsync();

		return await _context.Assessments
            .Include(a => a.Course)
            .Include(a => a.Module)
            .Where(a => enrolledCourseIds.Contains(a.CourseID))
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    public async Task<List<Result>> GetResultsByEmployeeAsync(int employeeId)
    {
        return await _context.Results
            .Where(r => r.EmployeeID == employeeId)
            .ToListAsync();
    }

    public async Task<List<Assessment>> GetAssessmentsForModuleAsync(int moduleId)
    {
        return await _context.Assessments
            .Include(a => a.Course)
            .Include(a => a.Module)
            .Where(a => a.ModuleID == moduleId)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    public async Task<Module?> GetModuleByIdAsync(int moduleId)
        => await _context.Modules.FindAsync(moduleId);

	public async Task<List<AuditLog>> GetPreviousScoreLogsAsync(int employeeId)
	{
		return await _context.AuditLogs
			.Where(a => a.UserID == employeeId && a.Action == "AssessmentPreviousScore")
			.ToListAsync();
	}
}
