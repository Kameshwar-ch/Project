using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillforge.Domain;

[Table("AuditLog")]
public class AuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AuditID{get; set;}

    [Column(TypeName ="INT")]
    public int ? UserID { get; set; }

    [ForeignKey("UserID")] 
    public virtual User UserIdNavigation { get; set; }
    public string Action{get; set;}

    [Column(TypeName ="VARCHAR(255)")]
    public string Resource{get; set;}

    [Column(TypeName ="DATETIME")]
    public DateTime Timestamp{get; set;}

}
