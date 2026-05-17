using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;

namespace Skillforge.Repository;

public class ReportRepository : IReportRepository
{
    private static readonly TimeZoneInfo IstZone =
        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    private static DateTime NowIst() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
    private readonly SkillForgeDB _context;

    public ReportRepository(SkillForgeDB context)
    {
        _context = context;
    }

    public async Task<ReportSchedule> CreateScheduleAsync(ReportSchedule schedule)
    {
        _context.ReportSchedules.Add(schedule);
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task<ReportSchedule?> GetScheduleByIdAsync(int scheduleId)
    {
        return await _context.ReportSchedules.FindAsync(scheduleId);
    }

    public async Task<IEnumerable<ReportSchedule>> GetAllSchedulesAsync()
    {
        return await _context.ReportSchedules.ToListAsync();
    }

    public async Task<IEnumerable<ReportSchedule>> GetDueSchedulesAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.ReportSchedules
            .Where(s => s.IsActive && s.NextRun <= now)
            .ToListAsync();
    }

    public async Task UpdateScheduleAsync(ReportSchedule schedule)
    {
        _context.ReportSchedules.Update(schedule);
        await _context.SaveChangesAsync();
    }

    public async Task SaveReportAsync(Report report)
    {
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();
    }
}
