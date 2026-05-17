using Skillforge.Domain;

namespace Skillforge.Repository;

/// <summary>
/// Defines the data access contract for assessment-related operations.
/// Abstracts database interactions for course lookups and assessment persistence.
/// </summary>
public interface IAssessmentRepository
{
    /// <summary>
    /// Retrieves a course by its unique identifier.
    /// Returns null if no matching course is found.
    /// </summary>
    /// <param name="courseId">The unique identifier of the course to retrieve.</param>
    /// <returns>The Course entity if found; otherwise null.</returns>
    Task<Course?> GetCourseByIdAsync(int courseId);

    /// <summary>
    /// Persists a new assessment to the database and returns the generated assessmentId.
    /// </summary>
    /// <param name="assessment">The Assessment entity to be inserted.</param>
    /// <returns>The auto-generated AssessmentID assigned by the database.</returns>
    Task<int> CreateAssessmentAsync(Assessment assessment);

    /// <summary>
    /// Retrieves all assessments with their associated course details.
    /// </summary>
    Task<List<Assessment>> GetAllAssessmentsAsync();
    Task<List<Assessment>> GetAssessmentsForEnrolledCoursesAsync(int employeeId);
    Task<List<Assessment>> GetAssessmentsForModuleAsync(int moduleId);
    Task<List<Result>> GetResultsByEmployeeAsync(int employeeId);
    Task<Module?> GetModuleByIdAsync(int moduleId);
	Task<List<AuditLog>> GetPreviousScoreLogsAsync(int employeeId);
}
