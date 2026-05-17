using System;
// using Microsoft.EntityFrameworkCore.;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Skillforge.Domain;

public enum AssessmentType
{
    Quiz,
    Exam,
    Practical
};
public class Assessment
{
    [Key]
    [Column(TypeName = "INT")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AssessmentID { get; set; }

    [Column(TypeName = "INT")]
    public int CourseID { get; set; }

    [ForeignKey("CourseID")]
    public virtual Course Course { get; set; }

    public int? ModuleID { get; set; }

    [ForeignKey("ModuleID")]
    public virtual Module? Module { get; set; }

    public AssessmentType Type { get; set; }

    [Column(TypeName = "DECIMAL(4,1)")]
    public decimal MaxScore { get; set; }

    [Column(TypeName = "DECIMAL(4,1)")]
    public decimal PassingScore { get; set; }

    [Column(TypeName = "DATETIME")]
    public DateTime Date { get; set; }

    public virtual ICollection<Result> Results { get; set; } = new List<Result>();
}
