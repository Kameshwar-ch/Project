using Skillforge.Domain;
using Skillforge.Dto;

namespace Skillforge.Service;

public interface IReportService
{
    Task<ReportScheduleResponseDto> CreateScheduleAsync(CreateReportScheduleDto dto, int adminId);
    Task<bool> DeactivateScheduleAsync(int scheduleId);
    Task<IEnumerable<ReportScheduleResponseDto>> GetAllSchedulesAsync();
    Task RunScheduledReportAsync(ReportSchedule schedule);

    /// <summary>
    /// Generates an ad-hoc report for the given scope, persists it to the
    /// database, sends a notification, and returns a styled PDF as bytes.
    /// </summary>
    Task<(byte[] PdfBytes, int ReportId)> GenerateReportAsync(GenerateReportRequestDto dto, int requestedById);
}
