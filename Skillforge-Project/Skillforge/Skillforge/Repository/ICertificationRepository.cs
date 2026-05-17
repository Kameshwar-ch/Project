using Skillforge.Domain;

namespace Skillforge.Repository;

/// <summary>
/// Defines data access operations required for certification issuance.
/// </summary>
public interface ICertificationRepository
{
    /// <summary>
    /// Asynchronously gets all the records from the certification table
    /// </summary>
    Task<List<Certification>> GetAllCertifications();

    /// <summary>Retrieves a user by ID; null if not found.</summary>
    Task<User?> GetUserByIdAsync(int userId);

    /// <summary>Retrieves a course by ID; null if not found.</summary>
    Task<Course?> GetCourseByIdAsync(int courseId);

    /// <summary>
    /// Returns true if the employee has a Pass result for any assessment
    /// belonging to the specified course.
    /// </summary>
    Task<bool> HasPassedAssessmentForCourseAsync(int employeeId, int courseId);

    /// <summary>
    /// Returns the existing Active certification for the given employee and course,
    /// or null if none exists.
    /// </summary>
    Task<Certification?> GetActiveCertificationAsync(int employeeId, int courseId);

    /// <summary>Persists the certification and returns the generated CertificationID.</summary>
    Task<int> IssueCertificationAsync(Certification certification);

    /// <summary>
    /// Returns Active certifications whose ExpiryDate falls within the next
    /// <paramref name="daysAhead"/> days (UTC, date-only comparison).
    /// Includes the Course navigation property for the course title.
    /// </summary>
    Task<List<Certification>> GetExpiringCertificationsAsync(int daysAhead);

    /// <summary>
    /// Returns certifications whose ExpiryDate has already passed but whose
    /// Status is still 'Active' — used by the background service to flip them
    /// to 'Expired' and notify the employee.
    /// Includes the Course navigation property for the course title.
    /// </summary>
    Task<List<Certification>> GetExpiredActiveCertificationsAsync();

    /// <summary>Updates the Status field of a single certification.</summary>
    Task UpdateStatusAsync(int certificationId, string status);
}
