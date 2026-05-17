namespace Skillforge.Dto;

public class EmployeeAssessmentDto
{
    public int AssessmentId { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int? ModuleId { get; set; }
    public string? ModuleName { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public DateTime Date { get; set; }
    public bool IsDone { get; set; }
    public int? ResultId { get; set; }
    public decimal? Score { get; set; }
    public string? ResultStatus { get; set; }
}
