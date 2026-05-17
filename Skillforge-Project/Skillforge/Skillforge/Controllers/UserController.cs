using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Dto;
using Skillforge.Service;
using Skillforge.Utility;
using Skillforge.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.Threading.Tasks;
namespace Skillforge.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        
        // Used as the canonical "users to pick from" source by HR (SkillGap
        // employee dropdown, Issue Cert employee dropdown — when applicable),
        // Manager (BulkEnroll employee picker), Trainer (course-related pickers),
        // and Admin (IAM). Employees do NOT need the org-wide user list.
        // UserResponseDto already excludes password / sensitive fields.
        [HttpGet("GetAll")]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.HR) + "," + nameof(UserRole.Manager) + "," + nameof(UserRole.Trainer))]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                List<UserResponseDto> users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Unable to fetch users." });
            }
        }
        /// <summary>
        /// Allows an Admin to update an existing user's information.
        /// Performs basic request validation and delegates business logic to the service layer.
        /// </summary>
        /// <param name="id">Receives the target userId from the route and update data from the request body.</param>
        /// <param name="request">update user request containing the feilds that allowed to be updated </param>
        /// <returns></returns>
        [HttpPut("update/{id}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                bool updated = await _userService.UpdateUser(id, request);

                if (!updated)
                {
                    return NotFound(UpdateMessages.NotFound);
                }

                return Ok(UpdateMessages.success);
            }
            catch (Exception)
            {
                return StatusCode(500, UpdateMessages.Error);
            }
        }

        [HttpPost("Register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UserRegister(UserRequestDto userRequestDto)
        {
            try
            {
                var (success, errorMessage) = await _userService.UserRegisterAsync(userRequestDto);

                // Return 400 if email already exists or any validation fails
                if (!success)
                    return BadRequest(new { message = errorMessage });

                return StatusCode(201, new { message = "User registered successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        [HttpPatch("{id}/status")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateStatusDto request)
        {
            try
            {
                bool updated = await _userService.UpdateUserStatus(id, request.Status);
                if (!updated)
                    return NotFound("User not found.");
                return Ok(new { message = "User status updated successfully." });
            }
            catch (Exception)
            {
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        // GET api/v1/User/me — returns the profile of the currently authenticated user
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var userIdClaim = User.FindFirstValue("id");
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = "Invalid token." });

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(user);
        }

        // GET api/v1/User/{id} — Admin fetches any user by ID
        [HttpGet("{id:int}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> GetUserById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID." });

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(user);
        }

        [HttpDelete("{userId}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest("Invalid User ID");
                }

                var deleted = await _userService.DeleteUser(userId);

                if (!deleted)
                    return NotFound(new { message = DeleteUserMessages.Delete.NotFound });

                return Ok(new { message = DeleteUserMessages.Delete.Success });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
