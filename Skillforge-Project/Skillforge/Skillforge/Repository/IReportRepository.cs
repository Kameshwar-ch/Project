using Skillforge.Domain;

namespace Skillforge.Repository;

public interface IReportRepository
{
    Task<ReportSchedule> CreateScheduleAsync(ReportSchedule schedule);
    Task<ReportSchedule?> GetScheduleByIdAsync(int scheduleId);
    Task<IEnumerable<ReportSchedule>> GetAllSchedulesAsync();
    Task<IEnumerable<ReportSchedule>> GetDueSchedulesAsync();
    Task UpdateScheduleAsync(ReportSchedule schedule);
    Task SaveReportAsync(Report report);
}
