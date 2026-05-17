namespace Skillforge.Dto;

// (IssueCertificationRequestDto removed — certificates are auto-issued, no
// manual request DTO needed.)

/// <summary>
/// Response DTO returned for cert lookups (own + org-wide list + PDF download).
/// </summary>
public class CertificationResponseDto
{
    public int CertificationId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseDescription { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
