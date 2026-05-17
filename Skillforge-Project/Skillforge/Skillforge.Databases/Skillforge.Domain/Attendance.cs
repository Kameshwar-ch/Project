using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillforge.Domain;

public enum AttendanceStatus
{
    Present,
    Absent
}

[Table("Attendance")]
public class Attendance
{
    [Key]
    [Column(TypeName = "INT")]
    public int AttendanceID { get; set; }
    [Required]
    [Column(TypeName = "INT")]
    public int EnrollmentID { get; set; }
    [ForeignKey("EnrollmentID")]
    public virtual Enrollment EnrollmentIdNavigation { get; set; }
    [Required]
    [Column(TypeName = "DATETIME")]
    public DateTime AttendanceDate { get; set; }
    [Column(TypeName = "VARCHAR(20)")]
    public AttendanceStatus Status { get; set; }
}


