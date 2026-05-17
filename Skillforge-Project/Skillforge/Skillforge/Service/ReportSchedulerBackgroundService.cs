using Skillforge.Repository;

namespace Skillforge.Service;

/// <summary>
/// Runs continuously in the background, checking every minute for report schedules
/// that are due and executing them automatically.
/// </summary>
public class ReportSchedulerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReportSchedulerBackgroundService> _logger;

    public ReportSchedulerBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReportSchedulerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Report Scheduler started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var reportRepo = scope.ServiceProvider.GetRequiredService<IReportRepository>();
                var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();

                var dueSchedules = await reportRepo.GetDueSchedulesAsync();

                foreach (var schedule in dueSchedules)
                {
                    _logger.LogInformation(
                        "Running scheduled report: ScheduleID={ScheduleID}, Scope={Scope}",
                        schedule.ScheduleID, schedule.Scope);

                    await reportService.RunScheduledReportAsync(schedule);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while running scheduled reports.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("Report Scheduler stopped.");
    }
}
