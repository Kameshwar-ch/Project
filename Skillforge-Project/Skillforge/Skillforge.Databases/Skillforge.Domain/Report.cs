using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillforge.Domain;

public enum ReportScope
{
    Course,
    Employee,
    Department
}

[Table("Report")]
public class Report
{
    [Key]
    public int ReportID { get; set; }

    [Required]
    public ReportScope Scope { get; set; }

    [Required]
    public string Metrics { get; set; }
    [Required]
    public DateTime GeneratedDate { get; set; }

    public int? ScheduleID { get; set; }

    [ForeignKey("ScheduleID")]
    public virtual ReportSchedule? Schedule { get; set; }
}
