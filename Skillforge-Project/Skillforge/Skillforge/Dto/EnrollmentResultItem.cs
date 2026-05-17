namespace Skillforge.Dto;

// Represents the enrollment result for a single employee in a bulk operation - includes status ("Success"/"Failed") and failure reason
public class EnrollmentResultItem
{
    public int EmployeeId { get; set; }
    public long? EnrollmentId { get; set; }
    public string Status { get; set; }
    public string? Reason { get; set; }
}
