namespace Skillforge.Dto;

// Response DTO for bulk enrollment - returns a summary with total, succeeded, failed counts and per-employee results
public class BulkEnrollmentResponseDto
{
    public int TotalRequested { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<EnrollmentResultItem> Results { get; set; } = new();
}
