using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Service;
using Skillforge.Dto;
using Skillforge.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace Skillforge.Controllers
{
    [Route("api/v1/Result")]
    [ApiController]
    public class ResultController : ControllerBase
    {
        private readonly IResultService _resultService;
        public ResultController(IResultService resultService)
        {
            _resultService = resultService;
        }
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Trainer))]
        public async Task<IActionResult> SubmitAssessmentResult([FromBody] SubmitAssessmentResultDto dto)
        {
            try
            {
                
                var claim = User.FindFirst("id");
                if (claim == null)
                {
                    return Unauthorized("User ID not found in token");
                }

                int reviewerUserId = int.Parse(claim.Value);

                await _resultService.SubmitResultAsync(dto, reviewerUserId);

                return StatusCode(201,new
                {
                    message = "Assessment result submitted successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new {message = ex.Message});
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("pending")]
        [Authorize(Roles = nameof(UserRole.Trainer))]
        public async Task<IActionResult> GetPendingResults()
        {
            var claim = User.FindFirst("id");
            if (claim == null)
                return Unauthorized(new { message = "Invalid token." });

            int trainerId = int.Parse(claim.Value);
            try
            {
                var results = await _resultService.GetPendingResultsAsync(trainerId);
                return Ok(results);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPatch("{assessmentId}/evaluate/{employeeId}")]
        [Authorize(Roles = nameof(UserRole.Trainer))]
        public async Task<IActionResult> EvaluateResult(int assessmentId, int employeeId, [FromBody] EvaluateResultDto dto)
        {
            var claim = User.FindFirst("id");
            if (claim == null)
                return Unauthorized(new { message = "Invalid token." });

            int trainerId = int.Parse(claim.Value);
            try
            {
                await _resultService.EvaluateResultAsync(assessmentId, employeeId, trainerId, dto.Pass);
                return Ok(new { message = "Result evaluated successfully." });
            }
            catch (KeyNotFoundException ex)     { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)                { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("self")]
        [Authorize(Roles = nameof(UserRole.Employee))]
        public async Task<IActionResult> SelfSubmitResult([FromBody] SelfSubmitResultDto dto)
        {
            var claim = User.FindFirst("id");
            if (claim == null)
                return Unauthorized(new { message = "Invalid token." });

            int employeeId = int.Parse(claim.Value);

            try
            {
                await _resultService.SelfSubmitAsync(dto.AssessmentId, dto.Score, employeeId);
                return StatusCode(201, new { message = "Assessment submitted successfully." });
            }
            catch (KeyNotFoundException ex)     { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)                { return BadRequest(new { message = ex.Message }); }
        }
    }
}
