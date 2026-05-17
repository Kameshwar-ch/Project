using Skillforge.Dto;

namespace Skillforge.Service;

/// <summary>
/// Certifications are auto-issued — there is no manual "issue" flow.
/// AutoIssueCertificationAsync is invoked by ModuleProgressService and
/// ResultService when a course is determined to be fully complete.
/// </summary>
public interface ICertificationService
{
    Task<List<CertificationResponseDto>> GetAllCertificationsAsync();
    Task<List<CertificationResponseDto>> GetMyCertificationsAsync(int employeeId);

    /// <summary>
    /// Auto-issues a certification on course completion. Idempotent — no-op if
    /// a cert already exists for this (employee, course) pair.
    /// </summary>
    Task AutoIssueCertificationAsync(int employeeId, int courseId, string courseTitle);
}
