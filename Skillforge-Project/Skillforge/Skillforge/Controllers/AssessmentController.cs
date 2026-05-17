using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Service;
using System.Security.Claims;

namespace Skillforge.Controller;

/// <summary>
/// AssessmentController manages course assessment lifecycle.
/// It provides endpoints for creating assessments tied to live courses, restricted to Trainers.
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
public class AssessmentController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;
    private readonly IAuditService _auditService;

    public AssessmentController(IAssessmentService assessmentService, IAuditService auditService)
    {
        _assessmentService = assessmentService;
        _auditService = auditService;
    }

    /// <summary>
    /// Creates a new assessment (Quiz, Exam, or Practical) for a specified live course.
    /// Validates that MaxScore is between 1 and 100 and that the target course is currently live.
    /// On success, logs an audit entry and returns the generated assessmentId.
    /// </summary>
    /// <param name="request">The assessment creation request containing CourseId, Type, and MaxScore.</param>
    /// <returns>
    /// 201 Created with the new assessmentId on success,
    /// 400 Bad Request if validation fails or the course is not live,
    /// or 500 Internal Server Error on unexpected failure.
    /// </returns>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAssessments()
    {
        try
        {
			var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
			var userIdClaim = User.FindFirstValue("id");
			int.TryParse(userIdClaim, out int userId);

			var result = await _assessmentService.GetAllAssessmentsAsync();

			// Trainers only see assessments for their own courses
			if (roleClaim == nameof(UserRole.Trainer))
				result = result.Where(a => a.TrainerID == userId).ToList();

			return Ok(result);
		}
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("module/{moduleId}")]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<IActionResult> GetAssessmentsByModule(int moduleId)
    {
        var employeeIdClaim = User.FindFirstValue("id");
        if (!int.TryParse(employeeIdClaim, out int employeeId))
            return Unauthorized(new { message = "Invalid token." });

        try
        {
            var result = await _assessmentService.GetAssessmentsByModuleAsync(moduleId, employeeId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("my")]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<IActionResult> GetMyAssessments()
    {
        var employeeIdClaim = User.FindFirstValue("id");
        if (!int.TryParse(employeeIdClaim, out int employeeId))
            return Unauthorized(new { message = "Invalid token." });

        try
        {
            var result = await _assessmentService.GetAssessmentsForEmployeeAsync(employeeId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("save-assessments")]
    [Authorize(Roles = nameof(UserRole.Trainer))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var trainerIdClaim = User.FindFirstValue("id");
            int.TryParse(trainerIdClaim, out int trainerId);

            var (success, errorMessage, assessmentId) = await _assessmentService.CreateAssessmentAsync(request);

            if (!success)
                return NotFound(new { message = errorMessage });

            await _auditService.LogAsync(trainerId, "AssessmentCreated", $"Assessment/{assessmentId}");

            return StatusCode(201, new { assessmentId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
