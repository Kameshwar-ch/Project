using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillforge.Domain;

[Table("ModuleProgress")]
public class ModuleProgress
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ProgressID { get; set; }

    public int EnrollmentID { get; set; }
    [ForeignKey("EnrollmentID")]
    public virtual Enrollment EnrollmentIdNavigation { get; set; }

    public int ModuleID { get; set; }
    [ForeignKey("ModuleID")]
    public virtual Module ModuleIdNavigation { get; set; }

    public DateTime CompletedAt { get; set; }
}
