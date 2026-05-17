using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillforge.Domain;

public class Certification
{
    [Key]
    [Column(TypeName = "INT")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CertificationID { get; set; }

    [Required]
    [Column(TypeName = "INT")]
    public int EmployeeID { get; set; }

    [ForeignKey("EmployeeID")]
    public virtual User UserRoleEmployee { get; set; }

    [Required]
    [Column(TypeName = "INT")]
    public int CourseID { get; set; }

    [ForeignKey("CourseID")]
    public virtual Course Course { get; set; }

    [Column(TypeName = "DATETIME")]
    public DateTime IssueDate { get; set; }

    [Column(TypeName = "DATETIME")]
    public DateTime ExpiryDate { get; set; }

    [Column(TypeName = "VARCHAR(20)")]
    public string Status { get; set; }
    
}
