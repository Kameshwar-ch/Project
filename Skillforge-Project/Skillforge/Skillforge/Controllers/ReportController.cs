using Cronos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Constants;
using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Service;
using System.Security.Claims;

namespace Skillforge.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Creates a recurring report schedule using a cron expression.
    /// The background service will automatically run the report at each scheduled interval
    /// and send a notification on completion.
    /// </summary>
    /// <param name="dto">Scope and cron expression for the schedule.</param>
    /// <returns>201 Created with schedule details including the next run time.</returns>
    
    [HttpPost("schedule")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(ReportScheduleResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateReportScheduleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var adminIdClaim = User.FindFirstValue("id");
            if (!int.TryParse(adminIdClaim, out int adminId))
                return Unauthorized(new { message = ReportErrorMessages.InvalidTokenClaims });

            var result = await _reportService.CreateScheduleAsync(dto, adminId);
            return StatusCode(201, result);
        }
        catch (CronFormatException)
        {
            return BadRequest(new { message = ReportErrorMessages.InvalidCronExpression });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Returns all report schedules.
    /// </summary>
    [HttpGet("schedules")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(IEnumerable<ReportScheduleResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedules()
    {
        var result = await _reportService.GetAllSchedulesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Deactivates a report schedule. The schedule is preserved for audit
    /// but the background service will no longer run it.
    /// </summary>
    [HttpPatch("schedules/{id}/deactivate")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> DeactivateSchedule(int id)
    {
        try
        {
            var success = await _reportService.DeactivateScheduleAsync(id);
            if (!success)
                return NotFound(new { message = "Schedule not found." });
            return Ok(new { message = "Schedule deactivated successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

/// <summary>
    /// Generates an ad-hoc report for the given scope immediately.
    /// Saves the report to the database, sends a notification, and returns
    /// a styled PDF file as a download.
    /// Accessible by Admin and HR.
    /// </summary>
    /// <param name="dto">Scope: Course | Employee | Department</param>
    [HttpPost("generate")]
	[Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.HR)},{nameof(UserRole.Manager)}")]
	[ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateReport([FromBody] GenerateReportRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userIdClaim = User.FindFirstValue("id");
            if (!int.TryParse(userIdClaim, out int requestedById))
                return Unauthorized(new { message = ReportErrorMessages.InvalidTokenClaims });

            var (pdfBytes, reportId) = await _reportService.GenerateReportAsync(dto, requestedById);

            var fileName = $"SkillForge-Report-{dto.Scope}-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
