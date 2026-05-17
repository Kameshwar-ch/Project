using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Dto.ComplianceRecordDto;
using Skillforge.Service;
using Skillforge.Utility;
using Skillforge.Domain;

namespace Skillforge.Controllers;

/// <summary>
/// Handles compliance-related API endpoints for HR dashboard.
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Roles = nameof(UserRole.HR))]
public class ComplianceRecordController : ControllerBase
{
    private readonly IComplianceRecordService _ComplianceRecordService;
    public ComplianceRecordController(IComplianceRecordService ComplianceRecordService)
    {
        _ComplianceRecordService = ComplianceRecordService;
    }

    /// <summary>
    /// Returns compliance summary including overall stats and individual records.
    /// </summary>
    /// <returns>ComplianceSummaryDto with employee compliance metrics and detailed records.</returns>
    [HttpGet("Summary")]
    public async Task<IActionResult> GetComplianceSummary()
    {
        try
        {
            ComplianceSummaryDto csd = await _ComplianceRecordService.GetComplianceSummaryAsync();
            return Ok(csd);
        }
        catch (DivideByZeroException)
        {
            return BadRequest(ComplianceRecordUtility.DivideByZero);
        }
        catch (Exception)
        {
            return StatusCode(500, ComplianceRecordUtility.FetchComplianceSummaryFailed);
        }
    }

    /// <summary>
    /// Re-evaluates all certification expiry dates and rebuilds the ComplianceRecord table.
    /// </summary>
    /// <returns>Success message string.</returns>
    [HttpGet("Refresh")]
    public async Task<ActionResult<string>> RefreshComplianceRecords()
    {
        try
        {
            return Ok(await _ComplianceRecordService.UpdateComplianceRecords());
        }
        catch (Exception)
        {
            return StatusCode(500, ComplianceRecordUtility.RefreshComplianceRecordsFailed);
        }
    }
}
