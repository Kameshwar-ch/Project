using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Service;
using Skillforge.Dto;
using Skillforge.Utility;
using Skillforge.Domain;

namespace Skillforge.Controller
{
    /// <summary>
    /// Controller for retrieving immutable audit logs.
    /// Filters: AuditID, UserID, Resource, Action, Timestamp.
    /// Sorting: AuditID, UserID, Resource, Action, Timestamp (asc/desc).
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }
        [HttpGet]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.HR))]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilterRequestDto request)
        {
            try
            {
                if (request.AuditID <= 0)
                    return StatusCode(400, new { message = AuditLogMessages.InvalidId });
                    
                var logs = await _auditLogService.GetAuditLogsAsync(request);

                if (logs.Items == null || !logs.Items.Any())
                    return StatusCode(404, new { message = AuditLogMessages.NoLogs });

                return StatusCode(200, new
                {
                    message = AuditLogMessages.Success,
                    data = logs.Items
                });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(401, new { message = "Unauthorized access" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = AuditLogMessages.Error, detail = ex.Message });
            }
        }
    }
}
