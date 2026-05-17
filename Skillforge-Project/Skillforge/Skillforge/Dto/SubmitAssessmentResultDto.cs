using System;

namespace Skillforge.Dto;

public class SubmitAssessmentResultDto
{
    public int AssessmentID { get; set; }
    public int EmployeeID { get; set; }
    public decimal Score { get; set; }
}

