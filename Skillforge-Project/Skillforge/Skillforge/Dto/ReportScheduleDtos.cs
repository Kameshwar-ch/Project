using System.ComponentModel.DataAnnotations;
using Skillforge.Domain;

namespace Skillforge.Dto;

public class CreateReportScheduleDto
{
    public ReportScope Scope { get; set; }

    /// <summary>
    /// Standard 5-field cron expression, e.g. "0 9 * * 1" = every Monday at 9 AM.
    /// </summary>
    public string CronExpression { get; set; }
}

public class ReportScheduleResponseDto
{
    public int ScheduleID { get; set; }
    public ReportScope Scope { get; set; }
    public string CronExpression { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? NextRun { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Request DTO for ad-hoc report generation.</summary>
public class GenerateReportRequestDto
{
    [Required]
    public ReportScope Scope { get; set; }
}

/// <summary>
/// Typed metrics collected for a report.
/// Null fields are not applicable for the given scope.
/// </summary>
public class ReportMetrics
{
    public string Scope { get; set; } = string.Empty;

    // Course scope
    public int? TotalCourses { get; set; }
    public int? ActiveCourses { get; set; }

    // Employee scope
    public int? TotalEmployees { get; set; }
    public int? CertifiedEmployees { get; set; }
    public int? CompliantEmployees { get; set; }
    public int? NonCompliantEmployees { get; set; }

    // Department scope (org-wide role breakdown)
    public int? TotalManagers { get; set; }
    public int? TotalTrainers { get; set; }
    public int? TotalHRs { get; set; }

    // Shared
    public int? TotalEnrollments { get; set; }
    public int? ActiveCertifications { get; set; }
    public double? ComplianceRate { get; set; }
    public int? TotalSkillGaps { get; set; }

    public DateTime GeneratedAt { get; set; }
}
