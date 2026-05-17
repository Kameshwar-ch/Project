using System.ComponentModel.DataAnnotations;

namespace Skillforge.Dto;

// Request DTO for manager bulk enrollment, accepts a course and list of employees to enroll
public class BulkEnrollmentRequestDto
{
    [Required]
    public int CourseId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one EmployeeId is required.")]
    public List<int> EmployeeIds { get; set; }
}
