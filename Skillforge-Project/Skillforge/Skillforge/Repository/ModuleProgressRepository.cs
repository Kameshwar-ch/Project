using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;

namespace Skillforge.Repository;

public class ModuleProgressRepository : IModuleProgressRepository
{
    private readonly SkillForgeDB _context;

    public ModuleProgressRepository(SkillForgeDB context)
    {
        _context = context;
    }

    public async Task<bool> IsCompletedAsync(int enrollmentId, int moduleId)
        => await _context.ModuleProgresses
            .AnyAsync(p => p.EnrollmentID == enrollmentId && p.ModuleID == moduleId);

    public async Task MarkCompleteAsync(int enrollmentId, int moduleId)
    {
        if (await IsCompletedAsync(enrollmentId, moduleId)) return;

        _context.ModuleProgresses.Add(new ModuleProgress
        {
            EnrollmentID = enrollmentId,
            ModuleID     = moduleId,
            CompletedAt  = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public async Task<List<int>> GetCompletedModuleIdsAsync(int enrollmentId)
        => await _context.ModuleProgresses
            .Where(p => p.EnrollmentID == enrollmentId)
            .Select(p => p.ModuleID)
            .ToListAsync();

    public async Task<int> GetCourseModuleCountAsync(int courseId)
        => await _context.Modules.CountAsync(m => m.CourseID == courseId);

    public async Task<int> GetCompletedModuleCountAsync(int enrollmentId, int courseId)
        => await _context.ModuleProgresses
            .Where(p => p.EnrollmentID == enrollmentId
                     && p.ModuleIdNavigation.CourseID == courseId)
            .CountAsync();

    public async Task<Enrollment?> GetEnrollmentAsync(int enrollmentId, int employeeId)
        => await _context.Enrollments
            .Include(e => e.CourseIdNavigation)
            .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId && e.EmployeeID == employeeId);

    public async Task<Module?> GetModuleAsync(int moduleId, int courseId)
        => await _context.Modules
            .FirstOrDefaultAsync(m => m.ModuleID == moduleId && m.CourseID == courseId);

    public async Task SetEnrollmentCompletedAsync(int enrollmentId)
    {
        var enrollment = await _context.Enrollments.FindAsync(enrollmentId);
        if (enrollment != null && !enrollment.CompletedDate.HasValue)
        {
            enrollment.CompletedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

	public async Task<(bool AllPassed, string? ErrorMessage)> CheckModuleAssessmentsAsync(int moduleId, int employeeId)
	{
		var assessments = await _context.Assessments
			.Where(a => a.ModuleID == moduleId)
			.ToListAsync();

		// No assessments for this module — allow completion
		if (!assessments.Any())
			return (true, null);

		var assessmentIds = assessments.Select(a => a.AssessmentID).ToList();

		var results = await _context.Results
			.Where(r => r.EmployeeID == employeeId && assessmentIds.Contains(r.AssessmentID))
			.ToListAsync();

		var resultMap = results.ToDictionary(r => r.AssessmentID);

		// Fetch previous best results from audit logs
		var previousLogs = await _context.AuditLogs
			.Where(a => a.UserID == employeeId && a.Action == "AssessmentPreviousScore")
			.ToListAsync();

		var bestPreviousStatus = new Dictionary<int, string>();
		foreach (var log in previousLogs)
		{
			var parts = log.Resource.Split('/');
			if (parts.Length >= 6 && int.TryParse(parts[1], out var aId))
			{
				var prevStatus = parts[5];
				if (prevStatus == "Pass")
					bestPreviousStatus[aId] = "Pass";
			}
		}

		foreach (var assessment in assessments)
		{
			if (!resultMap.TryGetValue(assessment.AssessmentID, out var result))
				return (false, "You have not submitted all assessments for this module. Please complete all assessments first.");

			// Check current result OR if they ever passed before
			if (result.Status != ResultStatus.Pass && !bestPreviousStatus.ContainsKey(assessment.AssessmentID))
				return (false, "You have not passed all assessments for this module. Pass all assessments before marking it as complete.");
		}

		return (true, null);
	}
}
