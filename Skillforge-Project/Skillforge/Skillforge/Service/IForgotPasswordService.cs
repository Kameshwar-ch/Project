using Skillforge.Dto;

namespace Skillforge.Service;

public interface IForgotPasswordService
{
    Task<ApiResponseDto> VerifyEmailAsync(ForgotPasswordRequestDto dto);
    Task<ApiResponseDto> ResetPasswordAsync(ResetPasswordDto dto);
}
