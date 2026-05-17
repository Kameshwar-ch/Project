using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Repository;
using Skillforge.Service;
using Skillforge.Data;

namespace Skillforge.Controller
{
	/// <summary>
	/// AuthController manages user identity and security sessions.
	/// It provides endpoints for credential verification, JWT token issuance, and security auditing.
	/// </summary>

	[Route("api/v1/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		/// <summary>
		/// Authenticates a user based on provided credentials.
		/// On success, it generates access and refresh tokens and logs a "Success" audit entry.
		/// On failure, it returns a 401 Unauthorized and logs a "Failed" audit entry.
		/// </summary>
		/// <param name="request">The login request containing Email and Password.</param>
		/// <returns>
		/// An IActionResult containing the JWT access_token, refresh_token, and expiry timestamp on success, 
		/// or an error message with appropriate HTTP status code on failure.
		/// </returns>

		// private readonly IUserService _userService;
		private readonly IUserRepository _userRepository;
		private readonly IAuditService _auditService;

		private readonly IJWTProviderService _jwtService;

		public AuthController(IUserRepository userRepository, IAuditService auditService, IJWTProviderService jwtService)
		{
			_userRepository = userRepository;
			_auditService = auditService;
			_jwtService = jwtService;
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			try
			{
				if (string.IsNullOrEmpty(request.Email) && string.IsNullOrEmpty(request.Password))
				{
					return BadRequest(new { message = "Email and Password are required.", StatusCode = 400 });
				}

				else if (string.IsNullOrEmpty(request.Email))
				{
					return BadRequest(new { message = "Email is required.", StatusCode = 400 });
				}

				else if (string.IsNullOrEmpty(request.Password))
				{
					return BadRequest(new { message = "Password is required.", StatusCode = 400 });
				}
				
				// authenticate function / method returns the user if the credentials are match else it will return null
				var user = await _userRepository.Authenticate(request.Email, request.Password);
				if (user == null)
				{
					// logAsync -> this will log the audits 
					await _auditService.LogAsync(null, "Login Failed", $"Email: {request.Email}");
					return Unauthorized(
						new
						{
							message = "UnAuthenticated User",
							StatusCode = 401
						}
					);
				}

				if (!user.Status)
				{
					await _auditService.LogAsync(user.UserID, "Login Blocked", "Inactive account");
					return Unauthorized(new { message = "Your account is inactive. Please contact support.", StatusCode = 401 });
				}
				// GenerateJwtToken this will generate JWT tokens and GenerateRefreshToken this will generate refresh token
				var accessToken = _jwtService.GenerateJwtToken(user);
				await _auditService.LogAsync(user.UserID, "Login SuccessFul", "Auth Endpoint");
				return Ok(
					new
					{
						access_token = accessToken,
						expires = DateTime.UtcNow.AddHours(1)
					}
				);
			}
			catch (System.Exception ex)
			{
				await _auditService.LogAsync(null, "Exception", ex.Message);
				return StatusCode(500, new { message = "An error occurred during login.", error = ex.Message });
			}
		}
	}
}