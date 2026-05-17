using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillforge.Domain;

public enum SkillGapLevel
{
    Low,
    Medium,
    High
}

[Table("SkillGap")]
public class SkillGap
{
    [Key]
    public int SkillGapID { get; set; }

    [Required]
    [Column(TypeName = "INT")]
    public int EmployeeID { get; set; }
    [ForeignKey("EmployeeID")]
    public virtual User Employee { get; set; }

    [Required]
    public int CompetencyID { get; set; }
    [ForeignKey("CompetencyID")]
    public virtual Competency Competency { get; set; }

    [Required]
    public SkillGapLevel GapLevel { get; set; }

    [Required]
    [Column(TypeName ="DATETIME")]
    public DateTime DateIdentified { get; set; }

}
