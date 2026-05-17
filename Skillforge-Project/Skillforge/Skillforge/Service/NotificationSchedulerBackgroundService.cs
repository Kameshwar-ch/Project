using Skillforge.Constants;
using Skillforge.Repository;

namespace Skillforge.Service;

/// <summary>
/// Runs daily and generates time-triggered notifications:
///   • Certification expiring in 7 days  → NotifyCertificationExpiringAsync
///   • Certification already expired     → NotifyCertificationExpiredAsync + flips Status to 'Expired'
///
/// Uses HasNotificationTodayAsync to skip certifications that were already
/// notified today, so a service restart never sends duplicate alerts.
/// </summary>
public class NotificationSchedulerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationSchedulerBackgroundService> _logger;

    private const int ExpiryWarningDays = 7;

    public NotificationSchedulerBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationSchedulerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Scheduler started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while running notification scheduler.");
            }

            // Run once per day
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }

        _logger.LogInformation("Notification Scheduler stopped.");
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var certRepo         = scope.ServiceProvider.GetRequiredService<ICertificationRepository>();
        var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var notificationSvc  = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await NotifyExpiringAsync(certRepo, notificationRepo, notificationSvc);
        await NotifyExpiredAsync(certRepo, notificationRepo, notificationSvc);
    }

    // ── Expiring in 7 days ───────────────────────────────────────────────────────

    private async Task NotifyExpiringAsync(
        ICertificationRepository certRepo,
        INotificationRepository  notificationRepo,
        INotificationService     notificationSvc)
    {
        var expiring = await certRepo.GetExpiringCertificationsAsync(ExpiryWarningDays);

        foreach (var cert in expiring)
        {
            var alreadyNotified = await notificationRepo.HasNotificationTodayAsync(
                cert.EmployeeID, cert.CertificationID, NotificationCategory.Certification);

            if (alreadyNotified)
            {
                _logger.LogInformation(
                    "Skipping expiry warning — already notified today. CertID={CertID}", cert.CertificationID);
                continue;
            }

            await notificationSvc.NotifyCertificationExpiringAsync(
                cert.EmployeeID,
                cert.CertificationID,
                cert.Course.Title,
                ExpiryWarningDays);

            _logger.LogInformation(
                "Expiry warning sent. EmployeeID={EmployeeID}, CertID={CertID}, Course={Course}",
                cert.EmployeeID, cert.CertificationID, cert.Course.Title);
        }
    }

    // ── Already expired ──────────────────────────────────────────────────────────

    private async Task NotifyExpiredAsync(
        ICertificationRepository certRepo,
        INotificationRepository  notificationRepo,
        INotificationService     notificationSvc)
    {
        var expired = await certRepo.GetExpiredActiveCertificationsAsync();

        foreach (var cert in expired)
        {
            var alreadyNotified = await notificationRepo.HasNotificationTodayAsync(
                cert.EmployeeID, cert.CertificationID, NotificationCategory.Certification);

            if (alreadyNotified)
            {
                _logger.LogInformation(
                    "Skipping expired notification — already notified today. CertID={CertID}", cert.CertificationID);
                continue;
            }

            // Flip Status to 'Expired' in the database
            await certRepo.UpdateStatusAsync(cert.CertificationID, "Expired");

            await notificationSvc.NotifyCertificationExpiredAsync(
                cert.EmployeeID,
                cert.CertificationID,
                cert.Course.Title);

            _logger.LogInformation(
                "Expiry notification sent and cert marked Expired. EmployeeID={EmployeeID}, CertID={CertID}",
                cert.EmployeeID, cert.CertificationID);
        }
    }
}
