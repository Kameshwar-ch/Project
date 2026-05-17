using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillforge.Domain;

[Table("Notification")]
public class Notification
{
    [Key]
    [Column(TypeName = "INT")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int NotificationID { get; set; }

    [Required]
    [Column(TypeName = "INT")]
    public int UserID { get; set; }

    [ForeignKey("UserID")]
    public virtual User User { get; set; }

    [Column(TypeName = "INT")]
    public int? CourseID { get; set; }

    [ForeignKey("CourseID")]
    public virtual Course? Course { get; set; }

    [Required]
    [Column(TypeName = "VARCHAR(255)")]
    public string Message { get; set; }

    [Required]
    [Column(TypeName = "VARCHAR(50)")]
    public string Category { get; set; }

    [Required]
    [Column(TypeName = "VARCHAR(10)")]
    public string Status { get; set; } = "Unread";

    [Required]
    [Column(TypeName = "DATETIME")]
    public DateTime CreatedDate { get; set; }
}
