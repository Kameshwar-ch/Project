using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Service;

namespace Skillforge.Controllers
{
	[Route("api/v1/[controller]")]
	[ApiController]
	public class AuditController : ControllerBase
	{
		private readonly IAuditCrudService _auditService;

		public AuditController(IAuditCrudService auditService)
		{
			_auditService = auditService;
		}

		[HttpGet]
		[Authorize(Roles = nameof(UserRole.HR) + "," + nameof(UserRole.Admin))]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var audits = await _auditService.GetAllAsync();
				return Ok(audits);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpPost]
		[Authorize(Roles = nameof(UserRole.HR))]
		public async Task<IActionResult> Create([FromBody] CreateAuditDto dto)
		{
			try
			{
				var hrIdClaim = User.FindFirstValue("id");
				if (!int.TryParse(hrIdClaim, out int hrId))
					return Unauthorized(new { message = "Invalid token." });

				var result = await _auditService.CreateAsync(dto, hrId);
				return StatusCode(201, result);
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpDelete("{id}")]
		[Authorize(Roles = nameof(UserRole.HR))]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				var deleted = await _auditService.DeleteAsync(id);
				if (!deleted) return NotFound(new { message = "Audit record not found." });
				return Ok(new { message = "Audit record deleted." });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}
	}
}
