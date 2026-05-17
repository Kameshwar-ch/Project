using Skillforge.Domain;

namespace Skillforge.Repository;

public interface IModuleProgressRepository
{
    Task<bool> IsCompletedAsync(int enrollmentId, int moduleId);
    Task MarkCompleteAsync(int enrollmentId, int moduleId);
    Task<List<int>> GetCompletedModuleIdsAsync(int enrollmentId);
    Task<int> GetCourseModuleCountAsync(int courseId);
    Task<int> GetCompletedModuleCountAsync(int enrollmentId, int courseId);
    Task<Enrollment?> GetEnrollmentAsync(int enrollmentId, int employeeId);
    Task<Module?> GetModuleAsync(int moduleId, int courseId);
    Task SetEnrollmentCompletedAsync(int enrollmentId);
    /// <summary>
    /// Returns (true, null) if the employee has passed all assessments for the module,
    /// or if the module has no assessments.
    /// Returns (false, errorMessage) otherwise.
    /// </summary>
    Task<(bool AllPassed, string? ErrorMessage)> CheckModuleAssessmentsAsync(int moduleId, int employeeId);
}
