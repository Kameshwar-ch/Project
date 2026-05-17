using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Skillforge.Domain;

[Table("Audit")]
public class Audit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AuditID { get; set; }

    // Mapped as string because UserID in the User table is VARCHAR(20)
    [Required]
    [Column(TypeName ="INT")]
    public int HRID { get; set; }

    [ForeignKey("HRID")]
    public virtual User HRUser {get;set;}

    [MaxLength(50)]
    public ReportScope Scope { get; set; }

    // No max length applied to allow for detailed audit findings (similar to NVARCHAR(MAX))
    public string Findings { get; set; }

    [Column(TypeName ="DATETIME")]
    public DateTime Date { get; set; }
    public bool Status { get; set; }

}
