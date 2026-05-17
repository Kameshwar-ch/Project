using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillforge.Domain;

public enum AttendanceRequestStatus
{
    Pending,
    Approved,
    Rejected
}

[Table("AttendanceRequests")]
public class AttendanceRequest
{
    [Key]
    public int RequestID { get; set; }

    [Required]
    public int EnrollmentID { get; set; }

    [ForeignKey("EnrollmentID")]
    public virtual Enrollment EnrollmentIdNavigation { get; set; } = null!;

    [Required]
    [Column(TypeName = "DATETIME")]
    public DateTime RequestDate { get; set; }

    [Required]
    public AttendanceRequestStatus Status { get; set; } = AttendanceRequestStatus.Pending;

    [Column(TypeName = "NVARCHAR(500)")]
    public string? TrainerNote { get; set; }

    [Required]
    [Column(TypeName = "DATETIME")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
