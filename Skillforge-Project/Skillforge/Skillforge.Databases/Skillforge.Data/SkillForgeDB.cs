using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Skillforge.Domain;
using DotNetEnv;

namespace Skillforge.Data;

public class SkillForgeDB : DbContext
{
    public SkillForgeDB()
    {

    }
    public SkillForgeDB(DbContextOptions<SkillForgeDB> options) : base(options)
    {

    }
    public virtual DbSet<Course> Courses { get; set; }
    public virtual DbSet<Module> Modules { get; set; }
    public virtual DbSet<Result> Results { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<AuditLog> AuditLogs { get; set; }
    public virtual DbSet<Attendance> Attendances { get; set; }
    public virtual DbSet<Competency> Competencies { get; set; }
    public virtual DbSet<Enrollment> Enrollments { get; set; }
    public virtual DbSet<Report> Reports { get; set; }
    public virtual DbSet<ReportSchedule> ReportSchedules { get; set; }
    public virtual DbSet<SkillGap> SkillGaps { get; set; }
    public virtual DbSet<Audit> Audits { get; set; }
    public virtual DbSet<Certification> Certifications { get; set; }
    public virtual DbSet<ComplianceRecord> ComplianceRecords { get; set; }
    public virtual DbSet<Assessment> Assessments { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<AttendanceRequest> AttendanceRequests { get; set; }
    public virtual DbSet<ModuleProgress> ModuleProgresses { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Path.Combine handles the slashes for you. 
            // Adding ".." twice moves the pointer two levels up the folder tree.
            string envPath = Path.Combine(Directory.GetCurrentDirectory(),"..","..",".env");

            DotNetEnv.Env.Load(envPath);

            var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            if (string.IsNullOrEmpty(conn))
            {
                // Helpful error if the file is found but the variable is missing
                throw new Exception($"Connection string not found in .env at: {envPath}");
            }

            optionsBuilder.UseSqlServer(conn);
        }

        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReportSchedule>(entity =>
        {
            entity.HasOne(rs => rs.Admin)
                .WithMany()
                .HasForeignKey(rs => rs.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(rs => rs.Scope)
                .HasConversion<string>()
                .HasColumnType("VARCHAR(20)");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.Property(r => r.Scope)
                .HasConversion<string>()
                .HasColumnType("VARCHAR(20)");

            entity.HasOne(r => r.Schedule)
                .WithMany(rs => rs.Reports)
                .HasForeignKey(r => r.ScheduleID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ComplianceRecord>()
        .HasOne(cr => cr.Certification)
        .WithMany()
        .HasForeignKey(cr => cr.CertificationID)
        .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Assessment>(entity =>
       {
           entity.HasOne(a => a.Course)
           .WithMany(c => c.Assessments)
           .HasForeignKey(a => a.CourseID)
           .OnDelete(DeleteBehavior.Cascade);

           entity.HasOne(a => a.Module)
           .WithMany(m => m.Assessments)
           .HasForeignKey(a => a.ModuleID)
           .OnDelete(DeleteBehavior.NoAction);

           entity.ToTable(t => t.HasCheckConstraint("CK_Assessment_MaxScore", "[MaxScore] >= 0 AND [MaxScore] <= 100"));

           entity.Property(a => a.MaxScore).HasPrecision(4, 1);

           entity.ToTable(t => t.HasCheckConstraint("CK_Assessment_Type", "[Type] IN ('Quiz', 'Exam', 'Practical')"));

       });

       
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Role)
            .HasConversion<string>()      // Enum → string
            .HasColumnType("VARCHAR(20)") // DB column type
            .IsRequired();
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.Property(e => e.Status)
            .HasConversion<string>()      // Enum → string
            .HasColumnType("VARCHAR(20)") // DB column type
            .IsRequired();
        });


        modelBuilder.Entity<Assessment>()
             .Property(a => a.Type)
             .HasConversion<string>()
             .HasColumnType("VARCHAR(20)");

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasOne(e => e.CourseIdNavigation)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EmployeeIdNavigation)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ModuleProgress>(entity =>
        {
            entity.HasOne(p => p.EnrollmentIdNavigation)
                .WithMany(e => e.ModuleProgresses)
                .HasForeignKey(p => p.EnrollmentID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.ModuleIdNavigation)
                .WithMany(m => m.ModuleProgresses)
                .HasForeignKey(p => p.ModuleID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => new { p.EnrollmentID, p.ModuleID }).IsUnique();
        });

        modelBuilder.Entity<Result>().HasKey(r => new { r.AssessmentID, r.EmployeeID });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.Property(r => r.Score).HasPrecision(4, 1);

            entity.HasOne(r => r.Assessment)
            .WithMany(a => a.Results)
            .HasForeignKey(r => r.AssessmentID)
            .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.UserRoleEmployee)
            .WithMany(u => u.Results)
            .HasForeignKey(r => r.EmployeeID)
            .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint("CK_Result_Score", "[Score] >= 0 AND [Score] <= 100"));

            entity.ToTable(t => t.HasCheckConstraint("CK_Result_Status", "[Status] IN ('Pass', 'Fail', 'Pending')"));
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasOne(c => c.UserIDNavigation)
                .WithMany(u => u.Courses)
                .HasForeignKey(c => c.TrainerID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.Modules)
                .WithOne(m => m.CourseIDNavigation)
                .HasForeignKey(m => m.CourseID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(a => a.UserIdNavigation)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Audit>(entity =>
        {
            entity.HasOne(a => a.HRUser)
                .WithMany(u => u.Audits)
                .HasForeignKey(a => a.HRID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SkillGap>(entity =>
        {
            entity.HasOne(s => s.Employee)
                .WithMany(u => u.SkillGaps)
                .HasForeignKey(s => s.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Competency)
                .WithMany(c => c.SkillGaps)
                .HasForeignKey(s => s.CompetencyID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComplianceRecord>(entity =>
        {
            entity.HasOne(c => c.Employee)
                .WithMany(u => u.ComplianceRecords)
                .HasForeignKey(c => c.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {    
            entity.Property(a => a.Status)
                .HasConversion<string>()       // Enum → string
                .HasColumnType("VARCHAR(20)")  // DB column type
                .IsRequired();
                
            entity.HasOne(a => a.EnrollmentIdNavigation)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EnrollmentID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AttendanceRequest>(entity =>
        {
            entity.Property(r => r.Status)
                .HasConversion<string>()
                .HasColumnType("VARCHAR(20)")
                .IsRequired();

            entity.HasOne(r => r.EnrollmentIdNavigation)
                .WithMany()
                .HasForeignKey(r => r.EnrollmentID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.Course)
                .WithMany()
                .HasForeignKey(n => n.CourseID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint("CK_Notification_Status", "[Status] IN ('Unread', 'Read')"));
        });

        modelBuilder.Entity<Certification>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint("CK_Certificate_Status", "[Status] IN ('Active', 'Revoked', 'Expired')"));

            entity.ToTable(t => t.HasCheckConstraint("CK_Certification_Dates",
            "[ExpiryDate] IS NULL OR [ExpiryDate] > [IssueDate]"));

            entity.HasOne(c => c.UserRoleEmployee)
            .WithMany(c => c.Certifications)
            .HasForeignKey(e => e.EmployeeID)
            .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Course)
            .WithMany(co => co.Certifications)
            .HasForeignKey(c => c.CourseID)
            .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
