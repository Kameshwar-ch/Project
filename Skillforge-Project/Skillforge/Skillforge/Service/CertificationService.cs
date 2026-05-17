using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Repository;

namespace Skillforge.Service;

/// <summary>
/// Certifications are issued automatically when a course is determined to be
/// fully complete (every module marked done AND every assessment passed).
/// There is no manual "issue" flow anymore.
/// </summary>
public class CertificationService : ICertificationService
{
    private readonly ICertificationRepository _certificationRepository;
    private readonly INotificationService _notificationService;

    public CertificationService(
        ICertificationRepository certificationRepository,
        INotificationService notificationService)
    {
        _certificationRepository = certificationRepository;
        _notificationService = notificationService;
    }

    public async Task AutoIssueCertificationAsync(int employeeId, int courseId, string courseTitle)
    {
        // Idempotent: bail if an active certification already exists.
        var existing = await _certificationRepository.GetActiveCertificationAsync(employeeId, courseId);
        if (existing != null) return;

        var issueDate = DateTime.UtcNow;
        var certification = new Certification
        {
            EmployeeID = employeeId,
            CourseID   = courseId,
            IssueDate  = issueDate,
            ExpiryDate = issueDate.AddYears(1),
            Status     = "Active"
        };
        int certificationId = await _certificationRepository.IssueCertificationAsync(certification);

        // Notify the employee. The repository handles persistence; the
        // notification service writes to the Notification table.
        await _notificationService.NotifyCertificationIssuedAsync(employeeId, certificationId);
    }

    public async Task<List<CertificationResponseDto>> GetMyCertificationsAsync(int employeeId)
    {
        var all = await GetAllCertificationsAsync();
        return all.Where(c => c.EmployeeId == employeeId).ToList();
    }

    public async Task<List<CertificationResponseDto>> GetAllCertificationsAsync()
    {
        var certs = await _certificationRepository.GetAllCertifications();
        var result = new List<CertificationResponseDto>();
        foreach (var c in certs)
        {
            var emp = await _certificationRepository.GetUserByIdAsync(c.EmployeeID);
            var course = await _certificationRepository.GetCourseByIdAsync(c.CourseID);
            result.Add(new CertificationResponseDto
            {
                CertificationId = c.CertificationID,
                EmployeeId = c.EmployeeID,
                EmployeeName = emp?.Name ?? string.Empty,
                CourseId = c.CourseID,
                CourseName = course?.Title ?? string.Empty,
                CourseDescription = course?.Description ?? string.Empty,
                IssueDate = c.IssueDate,
                ExpiryDate = c.ExpiryDate,
                Status = c.Status
            });
        }
        return result;
    }
}
