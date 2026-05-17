using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace Skillforge.Domain;

[Table("Course")]
public class Course
{
    [Key]
    [Column(TypeName = "INT")]
    public int CourseID { get; set; }
    [Column(TypeName = "VARCHAR(20)")]
    [Required]
    public string Title { get; set; }

    [Column(TypeName = "VARCHAR(50)")]
    [Required]
    public string Description { get; set; }

    [Column(TypeName = "INT")]

    public int TrainerID { get; set; }
    [ForeignKey("TrainerID")]
    public virtual User UserIDNavigation { get; set; }

    [Required]
    public decimal Duration { get; set; }

    public bool Status { get; set; }

    public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    public virtual ICollection<Certification> Certifications { get; set; } = new List<Certification>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
