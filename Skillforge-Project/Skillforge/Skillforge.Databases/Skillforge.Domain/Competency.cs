using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Skillforge.Domain;

public enum CompetencyLevel
{
    Beginner,
    Intermediate,
    Advanced
}

[Table("Competency")]
public class Competency
{
    [Key]
    public int CompetencyID { get; set; }
    [Required]
    [Column(TypeName = "VARCHAR(100)")]
    public string Name { get; set; }
    [Column(TypeName = "VARCHAR(200)")]
    public string Description { get; set; }
    [Required]
    [Column(TypeName = "VARCHAR(15)")]
    public CompetencyLevel Level { get; set; }
    public ICollection<SkillGap> SkillGaps { get; set; }
}

