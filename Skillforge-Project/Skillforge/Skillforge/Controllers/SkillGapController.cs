using Microsoft.AspNetCore.Mvc;
using Skillforge.Dto;
using Skillforge.Service;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Skillforge.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SkillGapController : ControllerBase
    {
        private readonly ISkillGapService _skillGapService;

        public SkillGapController(ISkillGapService skillGapService)
        {
            _skillGapService = skillGapService;
        }

        [HttpGet]
        [Authorize(Roles = "Manager,HR,Admin")]
        public async Task<IActionResult> GetSkillGaps(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? employeeId,
            [FromQuery] int? competencyId,
            [FromQuery] int? gapLevel)
        {
            if (startDate.HasValue && endDate.HasValue && endDate < startDate)
                return BadRequest("End date cannot be earlier than the start date.");

            try
            {
                var result = await _skillGapService.GetFilteredGapsAsync(
                    startDate, endDate, employeeId, competencyId, gapLevel);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An internal error occurred.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "HR,Admin")]
        public async Task<IActionResult> CreateSkillGap([FromBody] CreateSkillGapDto dto)
        {
            try
            {
                var result = await _skillGapService.CreateSkillGapAsync(dto);
                return StatusCode(201, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("employee/{employeeId:int}")]
        [Authorize(Roles = "Manager,HR,Admin")]
        public async Task<IActionResult> GetGapsByEmployee(int employeeId)
        {
            try
            {
                var result = await _skillGapService.GetGapsByEmployeeAsync(employeeId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An internal error occurred.");
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<IActionResult> DeleteSkillGap(int id)
        {
            try
            {
                bool deleted = await _skillGapService.DeleteSkillGapAsync(id);
                if (!deleted) return NotFound(new { message = "Skill gap not found." });
                return Ok(new { message = "Skill gap deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
