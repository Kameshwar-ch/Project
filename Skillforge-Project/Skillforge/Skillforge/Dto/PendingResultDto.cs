namespace Skillforge.Dto;

public class PendingResultDto
{
    public int AssessmentId { get; set; }
    public int EmployeeId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal PassingScore { get; set; }
}
