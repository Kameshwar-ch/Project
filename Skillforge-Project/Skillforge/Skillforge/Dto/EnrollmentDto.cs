using System;

namespace Skillforge.Dto;

public class EnrollmentDto
{
    public int CourseId { get; set; }
    public int EmployeeId { get; set; }
}

public class EnrollmentResponseDto
{
    public int EnrollmentId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string LastAttendance { get; set; } = "—";
}
