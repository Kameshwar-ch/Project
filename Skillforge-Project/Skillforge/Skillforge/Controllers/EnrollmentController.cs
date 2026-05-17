using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Service;
using System.Security.Claims;

namespace Skillforge.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class EnrollmentController : ControllerBase
{
    IEnrollmentService enrollmentService;
    private readonly IModuleProgressService _moduleProgressService;

    public EnrollmentController(IEnrollmentService _enrollmentService, IModuleProgressService moduleProgressService)
    {
        enrollmentService      = _enrollmentService;
        _moduleProgressService = moduleProgressService;
    }
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<IActionResult> Enroll(EnrollmentDto dto)
    {
        try
        {
            var id = await enrollmentService.EnrollAsync(dto.CourseId, dto.EmployeeId);
            return Created("", new { enrollmentId = id });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (BadHttpRequestException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    // GET api/v1/Enrollment - Returns all enrollments (Manager/Trainer/HR/Admin) or own enrollments (Employee)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetEnrollments()
    {
        try
        {
            var userIdClaim = User.FindFirstValue("id");
            var roleClaim = User.FindFirstValue(ClaimTypes.Role)
                           ?? User.FindFirstValue("role");

            int.TryParse(userIdClaim, out int userId);

            List<EnrollmentResponseDto> result;
            if (roleClaim == nameof(UserRole.Employee))
                result = await enrollmentService.GetEnrollmentsByEmployeeAsync(userId);
			else if (roleClaim == nameof(UserRole.Trainer))
				result = await enrollmentService.GetEnrollmentsByTrainerAsync(userId);
			else
                result = await enrollmentService.GetAllEnrollmentsAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // POST api/v1/Enrollment/{enrollmentId}/modules/{moduleId}/complete
    // Employee marks a module as complete; triggers course completion on the last module
    [HttpPost("{enrollmentId}/modules/{moduleId}/complete")]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<IActionResult> MarkModuleComplete(int enrollmentId, int moduleId)
    {
        var userIdClaim = User.FindFirstValue("id");
        if (!int.TryParse(userIdClaim, out int employeeId))
            return Unauthorized(new { message = "Invalid token." });

        try
        {
            var (success, message, courseCompleted) =
                await _moduleProgressService.MarkModuleCompleteAsync(enrollmentId, moduleId, employeeId);

            if (!success) return BadRequest(new { message });
            return Ok(new { message, courseCompleted });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // GET api/v1/Enrollment/{enrollmentId}/modules/progress
    // Returns list of completed module IDs for this enrollment
    [HttpGet("{enrollmentId}/modules/progress")]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<IActionResult> GetModuleProgress(int enrollmentId)
    {
        var userIdClaim = User.FindFirstValue("id");
        if (!int.TryParse(userIdClaim, out int employeeId))
            return Unauthorized(new { message = "Invalid token." });

        var completedIds = await _moduleProgressService.GetProgressAsync(enrollmentId, employeeId);
        return Ok(new { completedModuleIds = completedIds });
    }

    // PATCH api/v1/Enrollment/{enrollmentId}/status - Trainer approves or waitlists an enrollment
    [HttpPatch("{enrollmentId}/status")]
    [Authorize(Roles = nameof(UserRole.Trainer))]
    public async Task<IActionResult> UpdateEnrollmentStatus(int enrollmentId, [FromBody] UpdateStatusDto dto)
    {
        try
        {
            var (success, message) = await enrollmentService.UpdateEnrollmentStatusAsync(enrollmentId, dto.Status);
            if (!success) return NotFound(new { message });
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // POST api/v1/Enrollment/bulk - Manager assigns training to multiple employees
    // Returns 201 if all succeed, 200 for partial success, 400 if all fail
    [HttpPost("bulk")]
	[Authorize(Roles = nameof(UserRole.Manager) + "," + nameof(UserRole.HR) + "," + nameof(UserRole.Admin))]
	public async Task<IActionResult> BulkEnroll(BulkEnrollmentRequestDto dto)
    {
        try
        {
            // Extract manager ID from JWT token
            var userIdClaim = User.FindFirstValue("id");
            if (!int.TryParse(userIdClaim, out int managerId))
                return Unauthorized(new { message = "Invalid token." });

            var result = await enrollmentService.BulkEnrollAsync(dto, managerId);

            // All failed
            if (result.Succeeded == 0)
                return BadRequest(result);

            // Partial success
            if (result.Failed > 0)
                return Ok(result);

            // All succeeded
            return StatusCode(201, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (BadHttpRequestException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

