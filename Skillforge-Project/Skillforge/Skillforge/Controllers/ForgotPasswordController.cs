using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Dto;
using Skillforge.Service;

namespace Skillforge.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ForgotPasswordController : ControllerBase
{
    private readonly IForgotPasswordService _forgetPasswordService;

    public ForgotPasswordController(IForgotPasswordService forgotPasswordService)
    {
        _forgetPasswordService = forgotPasswordService;
    }

    // POST : User/forgotpassword/verifyemail
    [HttpPost("verifyemail")]
    public async Task<IActionResult> VerifyEmail([FromBody] ForgotPasswordRequestDto dto)
    {
        var result = await _forgetPasswordService.VerifyEmailAsync(dto);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // POST : User/forgotpassword/resetpassword
    [HttpPost("resetpassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto) 
    {
        var result = await _forgetPasswordService.ResetPasswordAsync(dto);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}