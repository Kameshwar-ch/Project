using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillforge.Domain;

[Table("ReportSchedule")]
public class ReportSchedule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ScheduleID { get; set; }

    [Required]
    public ReportScope Scope { get; set; }

    [Required]
    [MaxLength(100)]
    public string CronExpression { get; set; }

    [Required]
    public int CreatedBy { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User Admin { get; set; }

    [Column(TypeName = "DATETIME")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "DATETIME")]
    public DateTime? LastRun { get; set; }

    [Column(TypeName = "DATETIME")]
    public DateTime? NextRun { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Report> Reports { get; set; }
}
