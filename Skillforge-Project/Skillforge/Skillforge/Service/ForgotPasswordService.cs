using Skillforge.Dto;
using Skillforge.Repository;
using Skillforge.Utility;

namespace Skillforge.Service;

public class ForgotPasswordService : IForgotPasswordService
{
    private readonly IUserRepository UserRepository;

    public ForgotPasswordService(IUserRepository userRepository)
    {
        UserRepository = userRepository;
    }

    // Check if the email exists in the database
    public async Task<ApiResponseDto> VerifyEmailAsync(ForgotPasswordRequestDto dto)
    {
        // Did not enter the email to verify
        if (string.IsNullOrWhiteSpace(dto.Email))
            return ApiResponseDto.FailResponse(VerifyEmailUtility.EmailRequired);

        var user = await UserRepository.GetByEmailAsync(dto.Email);
        
        // Entered email account does not exist in database
        if (user == null)
            return ApiResponseDto.FailResponse(VerifyEmailUtility.EmailNotFound);

        return ApiResponseDto.SuccessResponse(VerifyEmailUtility.EmailFound);
    }

    //  Update the password
    public async Task<ApiResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
    {
        // Did not enter the email
        if (string.IsNullOrWhiteSpace(dto.Email))
            return ApiResponseDto.FailResponse(ResetPasswordUtility.EmailRequired);

        // Did not enter new Password
        if (string.IsNullOrWhiteSpace(dto.NewPassword))
            return ApiResponseDto.FailResponse(ResetPasswordUtility.EnterPassword);

        // new password and Re-enter password does not match 
        if (dto.NewPassword != dto.ConfirmPassword)
            return ApiResponseDto.FailResponse(ResetPasswordUtility.NoMatch);

        if (dto.NewPassword.Count() < 8)
            return ApiResponseDto.FailResponse(ResetPasswordUtility.PasswordLength);

        if (!(dto.NewPassword.Any(char.IsDigit)))
            return ApiResponseDto.FailResponse(ResetPasswordUtility.NoNumericInPassword);
        
        if (!(dto.NewPassword.Any(char.IsUpper)))
            return ApiResponseDto.FailResponse(ResetPasswordUtility.NoUppercaseInPassword);

        // Here once more we will check whether the Email entered for reset exists or not 
        var user = await UserRepository.GetByEmailAsync(dto.Email);
        if (user == null)
            return ApiResponseDto.FailResponse(VerifyEmailUtility.EmailNotFound);

        // The New password Entered Will be hashed here
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        var updated = await UserRepository.UpdatePasswordAsync(dto.Email, hashedPassword);

        return updated
            ? ApiResponseDto.SuccessResponse(ResetPasswordUtility.Updated)
            : ApiResponseDto.FailResponse(ResetPasswordUtility.Failed);
    }
}
