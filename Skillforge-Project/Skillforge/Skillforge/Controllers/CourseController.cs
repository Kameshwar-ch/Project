using Microsoft.AspNetCore.Mvc;
using Skillforge.Service;
using Skillforge.Dto;
using Skillforge.Domain;
using Microsoft.AspNetCore.Authorization;
using System;
using Skillforge.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Skillforge.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CourseController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        [HttpPost]
        // Requirement: Auth read from Enum (No hardcoding)
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Trainer))]
        public async Task<IActionResult> CreateCourse([FromBody] CourseRequestDto request)
        {
            try
            {
                await _courseService.CreateCourseAsync(request);
                return StatusCode(201, new { message = "Course successfully added." });
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
        [HttpGet("{cid}/modules")]
        [Authorize]
        public async Task<IActionResult> GetModules(int cid)
        {
            try
            {
                var modules = await _courseService.GetModulesByCourseAsync(cid);
                return Ok(modules);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("{cid}/modules")]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Trainer))]
        public async Task<IActionResult> AddModule(int cid, [FromBody] CreateModuleDto dto)
        {
            var userIdClaim = User.FindFirst("id")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? trainerId = null;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int id))
            {
                trainerId = id;
            }

            try
            {
                var moduleId = await _courseService.CreateModuleAsync(cid, dto, trainerId);

                return Ok(new
                {
                    Message = CourseMessages.ModuleCreated,
                    ModuleId = moduleId
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
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

        [HttpPut("{cid}/modules/{moduleId}")]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Trainer))]
        public async Task<IActionResult> UpdateModule(int cid, int moduleId, [FromBody] UpdateModuleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid request data." });
            try
            {
                bool updated = await _courseService.UpdateModuleAsync(cid, moduleId, dto);
                if (!updated)
                    return NotFound(new { message = "Module not found." });
                return Ok(new { message = "Module updated successfully." });
            }
            catch (ArgumentException ex)   { return BadRequest(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)           { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("{cid}/modules/{moduleId}")]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Trainer))]
        public async Task<IActionResult> DeleteModule(int cid, int moduleId)
        {
            try
            {
                bool deleted = await _courseService.DeleteModuleAsync(cid, moduleId);
                if (!deleted)
                    return NotFound(new { message = "Module not found." });
                return Ok(new { message = "Module deleted successfully." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)           { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("{courseID}")]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Trainer) + "," + nameof(UserRole.Employee)    )]
        public async Task<IActionResult> GetCourseByID(int courseID)
        {
            try
            {
                var claim = User.FindFirst("id");
                if (claim == null)
                    return Unauthorized(new { message = "UnAuthorize User." });

                int userID = int.Parse(claim.Value);

                var result = await _courseService.GetCourseByIDAsync(courseID, userID);
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
        [HttpGet]
        public async Task<IActionResult> GetCourses([FromQuery] CourseFilterRequestDto request)
        {
            var result = await _courseService.GetCoursesAsync(request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                bool deleted = await _courseService.DeleteCourseAsync(id);
                if (!deleted)
                    return NotFound(new { message = "Course not found." });
                return Ok(new { message = "Course deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Trainer))]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto)
        {
            try
            {
                bool updated = await _courseService.UpdateCourseAsync(id, dto);
                if (!updated)
                    return NotFound(new { message = "Course not found." });
                return Ok(new { message = "Course updated successfully." });
            }
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Trainer))]
        public async Task<IActionResult> UpdateCourseStatus(int id, [FromBody] UpdateStatusDto request)
        {
            try
            {
                bool updated = await _courseService.UpdateCourseStatus(id, request.Status);
                if (!updated)
                    return NotFound(new { message = "Course not found." });
                return Ok(new { message = "Course status updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}