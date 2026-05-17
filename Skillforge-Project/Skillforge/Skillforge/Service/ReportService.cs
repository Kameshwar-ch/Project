using Cronos;
using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Repository;
using System.Text.Json;

namespace Skillforge.Service;

public class ReportService : IReportService
{
    private static readonly TimeZoneInfo IstZone =
        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    private static DateTime NowIst() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);

    private readonly IReportRepository _reportRepository;
    private readonly INotificationService _notificationService;
    private readonly ReportPdfGenerator _pdfGenerator;
    private readonly SkillForgeDB _context;

    public ReportService(
        IReportRepository reportRepository,
        INotificationService notificationService,
        ReportPdfGenerator pdfGenerator,
        SkillForgeDB context)
    {
        _reportRepository    = reportRepository;
        _notificationService = notificationService;
        _pdfGenerator        = pdfGenerator;
        _context             = context;
    }

    public async Task<ReportScheduleResponseDto> CreateScheduleAsync(CreateReportScheduleDto dto, int adminId)
    {
        var cron = CronExpression.Parse(dto.CronExpression);
        var nextRun = cron.GetNextOccurrence(DateTime.UtcNow, IstZone);

        var schedule = new ReportSchedule
        {
            Scope = dto.Scope,
            CronExpression = dto.CronExpression,
            CreatedBy = adminId,
            CreatedAt = NowIst(),
            NextRun = nextRun,
            IsActive = true
        };

        await _reportRepository.CreateScheduleAsync(schedule);

        return MapToDto(schedule);
    }

    public async Task<IEnumerable<ReportScheduleResponseDto>> GetAllSchedulesAsync()
    {
        var schedules = await _reportRepository.GetAllSchedulesAsync();
        return schedules.Select(MapToDto);
    }

    public async Task<bool> DeactivateScheduleAsync(int scheduleId)
    {
        var schedule = await _context.ReportSchedules.FindAsync(scheduleId);
        if (schedule == null) return false;
        schedule.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }


public async Task RunScheduledReportAsync(ReportSchedule schedule)
    {
        var metrics = await BuildMetricsAsync(schedule.Scope);

        var report = new Report
        {
            Scope         = schedule.Scope,
            Metrics       = JsonSerializer.Serialize(metrics),
            GeneratedDate = NowIst(),
            ScheduleID    = schedule.ScheduleID
        };

        await _reportRepository.SaveReportAsync(report);

        await _notificationService.NotifyReportGeneratedAsync(
            schedule.CreatedBy, report.ReportID, schedule.Scope.ToString());

        var cron = CronExpression.Parse(schedule.CronExpression);
        schedule.LastRun = NowIst();
        schedule.NextRun = cron.GetNextOccurrence(DateTime.UtcNow, IstZone);

        await _reportRepository.UpdateScheduleAsync(schedule);
    }

    public async Task<(byte[] PdfBytes, int ReportId)> GenerateReportAsync(
        GenerateReportRequestDto dto, int requestedById)
    {
        var metrics = await BuildMetricsAsync(dto.Scope);

        var report = new Report
        {
            Scope         = dto.Scope,
            Metrics       = JsonSerializer.Serialize(metrics),
            GeneratedDate = metrics.GeneratedAt,
            ScheduleID    = null   // ad-hoc — not tied to a schedule
        };

        await _reportRepository.SaveReportAsync(report);

        await _notificationService.NotifyReportGeneratedAsync(
            requestedById, report.ReportID, dto.Scope.ToString());

        var pdfBytes = _pdfGenerator.Generate(metrics);

        return (pdfBytes, report.ReportID);
    }

    // ── Metrics builder (scope-aware) ────────────────────────────────────────────

    private async Task<ReportMetrics> BuildMetricsAsync(ReportScope scope)
    {
        // Shared aggregates — run all in parallel for performance on large data sets
        var totalEnrollments     = await _context.Enrollments.CountAsync();
        var activeCertifications = await _context.Certifications.CountAsync(c => c.Status == "Active");
        var totalCompliance      = await _context.ComplianceRecords.CountAsync();
        var compliantCount       = await _context.ComplianceRecords.CountAsync(c => c.Status == true);
        var complianceRate       = totalCompliance > 0
            ? Math.Round((double)compliantCount / totalCompliance * 100, 2)
            : 0;
        var totalSkillGaps = await _context.SkillGaps.CountAsync();

        var metrics = new ReportMetrics
        {
            Scope                = scope.ToString(),
            TotalEnrollments     = totalEnrollments,
            ActiveCertifications = activeCertifications,
            ComplianceRate       = complianceRate,
            TotalSkillGaps       = totalSkillGaps,
            GeneratedAt          = NowIst()
        };

        // Scope-specific extras
        switch (scope)
        {
            case ReportScope.Course:
                metrics.TotalCourses  = await _context.Courses.CountAsync();
                metrics.ActiveCourses = await _context.Courses.CountAsync(c => c.Status == true);
                break;

            case ReportScope.Employee:
                metrics.TotalEmployees       = await _context.Users.CountAsync(u => u.Role == UserRole.Employee && u.Status == true);
                metrics.CertifiedEmployees   = await _context.Certifications.Select(c => c.EmployeeID).Distinct().CountAsync();
                metrics.CompliantEmployees   = compliantCount;
                metrics.NonCompliantEmployees = totalCompliance - compliantCount;
                break;

            case ReportScope.Department:
                metrics.TotalEmployees        = await _context.Users.CountAsync(u => u.Role == UserRole.Employee && u.Status == true);
                metrics.TotalManagers         = await _context.Users.CountAsync(u => u.Role == UserRole.Manager  && u.Status == true);
                metrics.TotalTrainers         = await _context.Users.CountAsync(u => u.Role == UserRole.Trainer  && u.Status == true);
                metrics.TotalHRs              = await _context.Users.CountAsync(u => u.Role == UserRole.HR       && u.Status == true);
                metrics.CertifiedEmployees    = await _context.Certifications.Select(c => c.EmployeeID).Distinct().CountAsync();
                metrics.CompliantEmployees    = compliantCount;
                metrics.NonCompliantEmployees = totalCompliance - compliantCount;
                break;
        }

        return metrics;
    }

    private static ReportScheduleResponseDto MapToDto(ReportSchedule schedule) => new()
    {
        ScheduleID = schedule.ScheduleID,
        Scope = schedule.Scope,
        CronExpression = schedule.CronExpression,
        CreatedAt = schedule.CreatedAt,
        NextRun = schedule.NextRun,
        IsActive = schedule.IsActive
    };
}
