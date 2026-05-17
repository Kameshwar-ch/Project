using System;

namespace Skillforge.Dto;

// This DTO is used to take the response from user and update the database
public class ResetPasswordDto
{
    public string Email { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
