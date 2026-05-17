using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Skillforge.Domain;

public enum UserRole
{
    Employee,
    Trainer,
    Manager,
    HR,
    Admin
};

[Table("User")]
public class User
{
    [Key]
    [Column(TypeName = "INT")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserID { get; set; }

    [Column(TypeName = "VARCHAR(20)")]
    [Required]
    public string Name { get; set; }

    [Column(TypeName = "VARCHAR(20)")]
    [Required]
    public UserRole Role { get; set; }

    [Column(TypeName = "VARCHAR(50)")]
    [Required, RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
    ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; }  

    [Column(TypeName = "VARCHAR(10)")]
    [Required, RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits.")]
    public string Phone { get; set; }
    
    [Required]
    [Column(TypeName = "VARCHAR(255)")]
    public string Password { get; set; }
    

    [Required]
    public bool Status { get; set; }
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    public virtual ICollection<Result> Results { get; set; } = new List<Result>();
    public virtual ICollection<Certification> Certifications { get; set; } = new List<Certification>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<SkillGap> SkillGaps { get; set; } = new List<SkillGap>();
    public virtual ICollection<Audit> Audits { get; set; } = new List<Audit>();
    public virtual ICollection<ComplianceRecord> ComplianceRecords { get; set; } = new List<ComplianceRecord>();
}
