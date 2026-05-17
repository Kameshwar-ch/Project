using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillforge.Domain;

[Table("Enrollment")]
public class Enrollment
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int EnrollmentID { get; set; }
  public int CourseID { get; set; }
  [ForeignKey("CourseID")]
  public virtual Course CourseIdNavigation { get; set; }
  public int EmployeeID { get; set; }
  [ForeignKey("EmployeeID")]
  public virtual User EmployeeIdNavigation { get; set; }
  public DateTime EnrollmentDate { get; set; }
  public bool Status { get; set; }
  public DateTime? CompletedDate { get; set; }
  public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
  public virtual ICollection<ModuleProgress> ModuleProgresses { get; set; } = new List<ModuleProgress>();
}





