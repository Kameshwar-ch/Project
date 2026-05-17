using Skillforge.Constants;
using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Repository;

namespace Skillforge.Service;

/// <summary>
/// Persists notification records for every system-generated event.
/// Audit log entries are written only for events that do not already
/// log in their own service (e.g. Certification, Report).
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IAuditService _auditService;
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(IAuditService auditService, INotificationRepository notificationRepository)
    {
        _auditService           = auditService;
        _notificationRepository = notificationRepository;
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    private Task SaveAsync(int userId, int? courseId, string message, string category)
        => _notificationRepository.AddAsync(new Notification
        {
            UserID      = userId,
            CourseID    = courseId,
            Message     = message,
            Category    = category,
            Status      = "Unread",
            CreatedDate = DateTime.UtcNow
        });


    // ── READ ────────────────────────────────────────────────────────────────────

    public Task<List<NotificationResponseDto>> GetUnreadAsync(int userId, string? category)
        => _notificationRepository.GetUnreadAsync(userId, category);

    public Task<(NotificationResponseDto? Notification, bool AlreadyRead)> MarkAsReadAsync(int notificationId, int userId)
        => _notificationRepository.MarkAsReadAsync(notificationId, userId);


    // ── ENROLLMENT ──────────────────────────────────────────────────────────────

    public Task NotifyEnrollmentConfirmedAsync(int employeeId, int courseId, string courseTitle)
        => SaveAsync(
            employeeId, courseId,
            $"You have been enrolled in '{courseTitle}'.",
            NotificationCategory.Enrollment);

    public Task NotifyTrainerNewEnrollmentAsync(int trainerId, int courseId, string courseTitle, string employeeName)
        => SaveAsync(
            trainerId, courseId,
            $"{employeeName} has enrolled in your course '{courseTitle}'.",
            NotificationCategory.Enrollment);


    // ── ASSESSMENT ──────────────────────────────────────────────────────────────

    public Task NotifyAssessmentPassedAsync(int employeeId, int courseId, string courseTitle, decimal score)
        => SaveAsync(
            employeeId, courseId,
            $"Congratulations! You passed the assessment for '{courseTitle}' with a score of {score}.",
            NotificationCategory.Assessment);

    public Task NotifyAssessmentFailedAsync(int employeeId, int courseId, string courseTitle, decimal score)
        => SaveAsync(
            employeeId, courseId,
            $"You did not pass the assessment for '{courseTitle}'. Your score was {score}. Please review the material and try again.",
            NotificationCategory.Assessment);


    // ── CERTIFICATION ────────────────────────────────────────────────────────────

    public async Task NotifyCertificationIssuedAsync(int employeeId, int certificationId)
    {
        await _auditService.LogAsync(employeeId, "CertificationIssued", $"Certification/{certificationId}");
        await SaveAsync(
            employeeId, null,
            $"Your certification (ID: {certificationId}) has been issued.",
            NotificationCategory.Certification);
    }

    public Task NotifyCertificationExpiringAsync(int employeeId, int certificationId, string courseTitle, int daysLeft)
        => SaveAsync(
            employeeId, null,
            $"Your '{courseTitle}' certification (ID: {certificationId}) expires in {daysLeft} day(s). Please renew it.",
            NotificationCategory.Certification);

    public Task NotifyCertificationExpiredAsync(int employeeId, int certificationId, string courseTitle)
        => SaveAsync(
            employeeId, null,
            $"Your '{courseTitle}' certification (ID: {certificationId}) has expired.",
            NotificationCategory.Certification);

    public Task NotifyCertificationRevokedAsync(int employeeId, int certificationId, string courseTitle)
        => SaveAsync(
            employeeId, null,
            $"Your '{courseTitle}' certification (ID: {certificationId}) has been revoked. Please contact HR for details.",
            NotificationCategory.Certification);


    // ── REPORT ──────────────────────────────────────────────────────────────────

    public async Task NotifyReportGeneratedAsync(int adminId, int reportId, string scope)
    {
        await _auditService.LogAsync(adminId, "ReportGenerated", $"Report/{reportId}/Scope/{scope}");
        await SaveAsync(
            adminId, null,
            $"Report (ID: {reportId}) for scope '{scope}' has been generated.",
            NotificationCategory.Report);
    }


    // ── SKILL GAP ────────────────────────────────────────────────────────────────

    public Task NotifySkillGapIdentifiedAsync(int employeeId, string competencyName, string gapLevel)
        => SaveAsync(
            employeeId, null,
            $"A {gapLevel} skill gap in '{competencyName}' has been identified for you. Consider enrolling in a relevant course.",
            NotificationCategory.SkillGap);


    // ── COMPLIANCE ───────────────────────────────────────────────────────────────

    public Task NotifyComplianceNonCompliantAsync(int employeeId, string courseTitle)
        => SaveAsync(
            employeeId, null,
            $"You are non-compliant for '{courseTitle}'. Your certification has expired or been revoked. Please take action.",
            NotificationCategory.Compliance);


    // ── ATTENDANCE REQUEST ───────────────────────────────────────────────────────

    public Task NotifyAttendanceRequestReviewedAsync(int employeeId, int courseId, string courseTitle, bool approved, string? note)
    {
        var outcome = approved ? "approved" : "rejected";
        var noteText = !string.IsNullOrEmpty(note) ? $" Trainer note: {note}" : string.Empty;
        return SaveAsync(
            employeeId, courseId,
            $"Your attendance request for '{courseTitle}' was {outcome}.{noteText}",
            NotificationCategory.Enrollment);
    }


    // ── COURSE COMPLETION ────────────────────────────────────────────────────────

    public Task NotifyCourseCompletedAsync(int employeeId, int courseId, string courseTitle)
        => SaveAsync(
            employeeId, courseId,
            $"Congratulations! You have completed '{courseTitle}'. Your certificate is now available for download.",
            NotificationCategory.Certification);


    // ── AUDIT ────────────────────────────────────────────────────────────────────

    public Task NotifyAuditCompletedAsync(int hrId, int auditId, string scope)
        => SaveAsync(
            hrId, null,
            $"Audit (ID: {auditId}) for scope '{scope}' has been recorded successfully.",
            NotificationCategory.Audit);
}
