using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Skillforge.Domain;
public enum ResultStatus
{
    Pending,
    Pass,
    Fail
};
public class Result
{
    [Column(TypeName = "INT")]
    public int ResultID { get; set; }

    [Required]
    [Column(TypeName = "INT")]
    public int AssessmentID { get; set; }

    [ForeignKey("AssessmentID")]
    public virtual Assessment Assessment { get; set; }

    [Required]
    [Column(TypeName = "INT")]
    public int EmployeeID { get; set; }

    [ForeignKey("EmployeeID")]
    public virtual User UserRoleEmployee { get; set; }

    [Column(TypeName = "DECIMAL(4,1)")]
    public decimal Score { get; set; }
    [Column(TypeName = "VARCHAR(20)")]
    public ResultStatus Status { get; set; }
}
