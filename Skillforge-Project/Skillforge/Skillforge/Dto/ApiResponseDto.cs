using System;

namespace Skillforge.Dto;

public class ApiResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    // This will just return the object of DTO with required Result type
    public static ApiResponseDto SuccessResponse(string message)
    {
        return new ApiResponseDto { Success = true, Message = message};
    }

    public static ApiResponseDto FailResponse(string message)
    {
        return new ApiResponseDto {Success = false, Message = message};
    }
}
