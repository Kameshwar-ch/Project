using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Skillforge.Domain;

[Table("ComplianceRecord")]
public class ComplianceRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ComplianceID { get; set; }

    [Required]
    public int EmployeeID { get; set; }
    [ForeignKey("EmployeeID")]
    public virtual User Employee { get; set; }

    [Required]
    public int CertificationID { get; set; }
    [ForeignKey("CertificationID")]
    public virtual Certification Certification { get; set; }

    public bool Status { get; set; }

    // this will be todays date time
    [Column(TypeName ="DATETIME")]
    public DateTime Date { get; set; }

}
