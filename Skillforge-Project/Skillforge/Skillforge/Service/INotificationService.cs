using Skillforge.Dto;

namespace Skillforge.Service;

/// <summary>
/// Defines every system-generated notification the application can produce.
/// Organised by feature area: Enrollment → Assessment → Certification → Report → SkillGap → Compliance → Audit.
/// </summary>
public interface INotificationService
{
    // ── READ / WRITE ─────────────────────────────────────────────────────────────

    /// <summary>Returns all unread notifications for the given user, optionally filtered by category.</summary>
    Task<List<NotificationResponseDto>> GetUnreadAsync(int userId, string? category);

    /// <summary>
    /// Marks a notification as Read.
    /// Returns (null, false)  → not found or wrong owner.
    /// Returns (dto,  true)   → was already Read.
    /// Returns (dto,  false)  → just marked as Read.
    /// </summary>
    Task<(NotificationResponseDto? Notification, bool AlreadyRead)> MarkAsReadAsync(int notificationId, int userId);


    // ── ENROLLMENT ──────────────────────────────────────────────────────────────

    /// <summary>Notifies the employee that they have been enrolled in a course.</summary>
    Task NotifyEnrollmentConfirmedAsync(int employeeId, int courseId, string courseTitle);

    /// <summary>Notifies the trainer that a new student has enrolled in their course.</summary>
    Task NotifyTrainerNewEnrollmentAsync(int trainerId, int courseId, string courseTitle, string employeeName);


    // ── ASSESSMENT ──────────────────────────────────────────────────────────────

    /// <summary>Notifies the employee that they passed an assessment.</summary>
    Task NotifyAssessmentPassedAsync(int employeeId, int courseId, string courseTitle, decimal score);

    /// <summary>Notifies the employee that they failed an assessment.</summary>
    Task NotifyAssessmentFailedAsync(int employeeId, int courseId, string courseTitle, decimal score);


    // ── CERTIFICATION ────────────────────────────────────────────────────────────

    /// <summary>Notifies the employee that their certification has been issued.</summary>
    Task NotifyCertificationIssuedAsync(int employeeId, int certificationId);

    /// <summary>Notifies the employee that their certification is expiring soon (e.g. in 7 days).</summary>
    Task NotifyCertificationExpiringAsync(int employeeId, int certificationId, string courseTitle, int daysLeft);

    /// <summary>Notifies the employee that their certification has expired.</summary>
    Task NotifyCertificationExpiredAsync(int employeeId, int certificationId, string courseTitle);

    /// <summary>Notifies the employee that their certification has been revoked.</summary>
    Task NotifyCertificationRevokedAsync(int employeeId, int certificationId, string courseTitle);


    // ── REPORT ──────────────────────────────────────────────────────────────────

    /// <summary>Notifies the admin that a report (scheduled or manual) has been generated.</summary>
    Task NotifyReportGeneratedAsync(int adminId, int reportId, string scope);


    // ── SKILL GAP ────────────────────────────────────────────────────────────────

    /// <summary>Notifies the employee that a new skill gap has been identified for them.</summary>
    Task NotifySkillGapIdentifiedAsync(int employeeId, string competencyName, string gapLevel);


    // ── COMPLIANCE ───────────────────────────────────────────────────────────────

    /// <summary>Notifies the employee that they are non-compliant due to an expired/revoked certification.</summary>
    Task NotifyComplianceNonCompliantAsync(int employeeId, string courseTitle);


    // ── ATTENDANCE REQUEST ───────────────────────────────────────────────────────

    /// <summary>Notifies the employee that their attendance request was approved or rejected.</summary>
    Task NotifyAttendanceRequestReviewedAsync(int employeeId, int courseId, string courseTitle, bool approved, string? note);


    // ── COURSE COMPLETION ────────────────────────────────────────────────────────

    /// <summary>Notifies the employee that they have completed all modules and their certificate is ready.</summary>
    Task NotifyCourseCompletedAsync(int employeeId, int courseId, string courseTitle);


    // ── AUDIT ────────────────────────────────────────────────────────────────────

    /// <summary>Notifies the HR user that an audit they performed has been recorded.</summary>
    Task NotifyAuditCompletedAsync(int hrId, int auditId, string scope);
}
