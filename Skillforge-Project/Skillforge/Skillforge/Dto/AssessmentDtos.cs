using System.ComponentModel.DataAnnotations;
using Skillforge.Domain;

namespace Skillforge.Dto;

/// <summary>
/// Request DTO for creating a new assessment.
/// Contains the target course, assessment type, and maximum achievable score.
/// </summary>
public class CreateAssessmentRequestDto
{
    [Required]
    public int CourseId { get; set; }

    public int? ModuleId { get; set; }

    [Required]
    public AssessmentType Type { get; set; }

    /// <summary>
    /// The maximum score achievable in this assessment. Must be between 1 and 100.
    /// </summary>
    [Required]
    [Range(1, 100, ErrorMessage = "MaxScore must be between 1 and 100.")]
    public decimal MaxScore { get; set; }

    /// <summary>
    /// The minimum score required to pass. Must be between 1 and MaxScore.
    /// Hidden from employees — used only for Pass/Fail calculation.
    /// </summary>
    [Required]
    [Range(1, 100, ErrorMessage = "Passing score must be between 1 and 100.")]
    public decimal PassingScore { get; set; }
}

/// <summary>
/// Response DTO returned after successfully creating an assessment.
/// Contains the auto-generated identifier for the new assessment.
/// </summary>
public class CreateAssessmentResponseDto
{
    public int AssessmentId { get; set; }
}

/// <summary>
/// Response DTO used for listing assessments with course details (Trainer/Admin only).
/// Includes PassingScore which is hidden from employees.
/// </summary>
public class AssessmentResponseDto
{
    public int AssessmentId { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int? ModuleId { get; set; }
    public string? ModuleName { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal PassingScore { get; set; }
    public DateTime Date { get; set; }
	public bool CourseStatus { get; set; }

	public int TrainerID { get; set; }
}
