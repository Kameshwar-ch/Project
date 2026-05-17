using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Dto;
using Skillforge.Domain;
using Skillforge.Service;

namespace Skillforge.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    /// <summary>
    /// Initializes AttendanceController with the attendance service.
    /// </summary>
    /// <param name="attendanceService">Service for handling attendance operations.</param>
    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    /// <summary>
    /// Marks attendance for single or bulk enrollments in a course.
    /// Send one record for single, multiple records for bulk.
    /// </summary>
    [HttpPost("Mark-Attendance")]
    [Authorize(Roles = nameof(UserRole.Trainer))]
    public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid request data." });

        try
        {
            var claim = User.FindFirst("id");
            if (claim == null)
                return Unauthorized(new { message = "Unauthorize user." });

            int trainerID = int.Parse(claim.Value);

            var result = await _attendanceService.MarkAttendanceAsync(dto, trainerID);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    // ── Attendance History ────────────────────────────────────────────────────

    /// <summary>Returns all attendance records for a specific enrollment. Accessible by the enrolled employee, the course trainer, and Manager/HR/Admin.</summary>
    [HttpGet("enrollment/{enrollmentId}")]
    [Authorize]
    public async Task<IActionResult> GetAttendanceHistory(int enrollmentId)
    {
        try
        {
            var claim = User.FindFirst("id");
            if (claim == null) return Unauthorized(new { message = "Unauthorized user." });
            int userId = int.Parse(claim.Value);

            var roleClaim = User.FindFirst("role")?.Value ?? string.Empty;

            var result = await _attendanceService.GetAttendanceHistoryAsync(enrollmentId, userId, roleClaim);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
        catch (Exception ex)                   { return StatusCode(500, new { message = ex.Message }); }
    }

    // ── Attendance Request Endpoints ─────────────────────────────────────────

    /// <summary>Employee submits an attendance request for a specific date.</summary>
    [HttpPost("request")]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<IActionResult> CreateAttendanceRequest([FromBody] CreateAttendanceRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid request data." });

        try
        {
            var claim = User.FindFirst("id");
            if (claim == null) return Unauthorized(new { message = "Unauthorized user." });
            int employeeId = int.Parse(claim.Value);

            var result = await _attendanceService.CreateAttendanceRequestAsync(dto, employeeId);
            return StatusCode(201, result);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex)   { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)                   { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>Employee retrieves their own attendance requests.</summary>
    [HttpGet("requests/my")]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<IActionResult> GetMyAttendanceRequests()
    {
        try
        {
            var claim = User.FindFirst("id");
            if (claim == null) return Unauthorized(new { message = "Unauthorized user." });
            int employeeId = int.Parse(claim.Value);

            var result = await _attendanceService.GetMyAttendanceRequestsAsync(employeeId);
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>Trainer retrieves all pending attendance requests for a course they own.</summary>
    [HttpGet("requests/course/{courseId}/pending")]
    [Authorize(Roles = nameof(UserRole.Trainer))]
    public async Task<IActionResult> GetPendingRequests(int courseId)
    {
        try
        {
            var claim = User.FindFirst("id");
            if (claim == null) return Unauthorized(new { message = "Unauthorized user." });
            int trainerId = int.Parse(claim.Value);

            var result = await _attendanceService.GetPendingRequestsForCourseAsync(courseId, trainerId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
        catch (Exception ex)                   { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>Trainer approves or rejects an attendance request. Approval auto-marks attendance as Present.</summary>
    [HttpPatch("requests/{requestId}/review")]
    [Authorize(Roles = nameof(UserRole.Trainer))]
    public async Task<IActionResult> ReviewAttendanceRequest(int requestId, [FromBody] ReviewAttendanceRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid request data." });

        try
        {
            var claim = User.FindFirst("id");
            if (claim == null) return Unauthorized(new { message = "Unauthorized user." });
            int trainerId = int.Parse(claim.Value);

            var result = await _attendanceService.ReviewAttendanceRequestAsync(requestId, dto, trainerId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex)   { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)                   { return StatusCode(500, new { message = ex.Message }); }
    }

    //  GET Course Attendance

    /// <summary>
    /// Retrieves attendance preview for all active enrollments in a course on a specific date.
    /// Shows CourseStatus (Accessed/Not Accessed) and LoginDate from AuditLog per employee.
    /// Trainer uses this to review before marking attendance.
    /// </summary>
    /// <param name="courseID">ID of the course.</param>
    /// <param name="date">Date to retrieve attendance for.</param>
    /// <returns>List of employees with CourseStatus and LoginDate.</returns>
    // GET /api/attendance/course/{courseID}?date=2026-04-20
    [HttpGet("course/{courseID}")]
    [Authorize(Roles = nameof(UserRole.Trainer))]
    public async Task<IActionResult> GetCourseAttendance(int courseID, [FromQuery] DateTime date)
    {
        try
        {
            var claim = User.FindFirst("id");
            if (claim == null)
                return Unauthorized(new { message = "UnAuthorize User.." });

            int trainerID = int.Parse(claim.Value);

            var result = await _attendanceService.GetCourseAttendanceAsync(courseID, date, trainerID);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
        }
    }
}