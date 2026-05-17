using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Dto;
using Skillforge.Service;
using Skillforge.Utility;

namespace Skillforge.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CompetencyController : ControllerBase
    {
        private readonly ICompetencyService _competencyService;

        public CompetencyController(ICompetencyService competencyService)
        {
            _competencyService = competencyService;
        }

        [HttpGet("matrix")]
        [Authorize(Roles = "Manager,HR,Admin")]
        public async Task<IActionResult> GetCompetencyMatrix([FromQuery] CompetencyMatrixSearchDto searchDto)
        {
            var result = await _competencyService.GetCompetencyMatrixAsync(searchDto);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Manager,HR,Admin")]
        public async Task<IActionResult> GetAllCompetencies()
        {
            try
            {
                var result = await _competencyService.GetAllCompetenciesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "HR,Admin")]
        public async Task<IActionResult> CreateCompetency([FromBody] CreateCompetencyDto dto)
        {
            try
            {
                var result = await _competencyService.CreateCompetencyAsync(dto);
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

        [HttpPut("{id:int}")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<IActionResult> UpdateCompetency(int id, [FromBody] UpdateCompetencyDto dto)
        {
            try
            {
                bool updated = await _competencyService.UpdateCompetencyAsync(id, dto);
                if (!updated) return NotFound(new { message = "Competency not found." });
                return Ok(new { message = "Competency updated successfully." });
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

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<IActionResult> DeleteCompetency(int id)
        {
            try
            {
                bool deleted = await _competencyService.DeleteCompetencyAsync(id);
                if (!deleted) return NotFound(new { message = "Competency not found." });
                return Ok(new { message = "Competency deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
