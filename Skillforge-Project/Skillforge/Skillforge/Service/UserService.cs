using System;
using System.Diagnostics.Eventing.Reader;
using Microsoft.AspNetCore.Identity;
using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Repository;
namespace Skillforge.Service;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    /// <summary>
    /// Updates an existing user's information based on the provided userId.
    /// Retrieves the user from the repository, applies the allowed updates,
    /// and persists the changes. Returns false if the user does not exist.
    /// </summary>
    /// <param name="request">
    /// DTO containing the user details that are allowed to be updated, including the Id.
    /// </param>
    /// <returns>
    /// True if the user was successfully updated; false if the user was not found.
    /// </returns>
    public async Task<bool> UpdateUser(int id, UpdateUserRequestDto request)
    {
        var user = await _userRepository.GetUserByIdAsync(id);

        if (user == null)
            return false;

        user.Name = request.Name;
        user.Role = request.Role;
        user.Phone = request.Phone;
        user.Status = request.Status;

        return await _userRepository.UpdateUser(user);

    }
    public async Task<bool> UpdateUserStatus(int id, bool status)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null) return false;
        user.Status = status;
        return await _userRepository.UpdateUser(user);
    }

    public async Task<bool> DeleteUser(int userId)
    {
        return await _userRepository.DeleteUser(userId);
    }
    public async Task<List<UserResponseDto>> GetAllUsersAsync()
    {
        List<User> users;
        try
        {
            users = await _userRepository.GetAllUsersAsync();
        }
        catch (Exception ex)
        {
            throw new Exception(Utility.ErrorMessages.UsersNotFound);
        }
        List<UserResponseDto> userResponseDtos = new List<UserResponseDto>();
        foreach (User user in users)
        {
            UserResponseDto responseDto = new UserResponseDto
            {
                UserID = user.UserID,
                UserName = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                RoleName = user.Role,
                Status = user.Status
            };
            userResponseDtos.Add(responseDto);
        }
        return userResponseDtos;
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null) return null;

        return new UserResponseDto
        {
            UserID    = user.UserID,
            UserName  = user.Name,
            Email     = user.Email,
            Phone     = user.Phone,
            RoleName  = user.Role,
            Status    = user.Status
        };
    }

    public async Task<(bool Success, string ErrorMessage)> UserRegisterAsync(UserRequestDto userRequestDto)
    {
        //block duplicate email registrations before doing any DB write
        var existingUser = await _userRepository.GetByEmailAsync(userRequestDto.Email!);

        if (existingUser != null && existingUser.Status)
            return (false, "Email is already registered.");

        if (existingUser != null && !existingUser.Status)
            return (false, "Your account is inactive. Please contact support.");
        // Map DTO → Domain model, assigning default role and hashing the password
        if (!Enum.TryParse<UserRole>(userRequestDto.Role, ignoreCase: true, out var parsedRole))
            return (false, $"Invalid role '{userRequestDto.Role}'.");

        var userModel = new User
        {
            Name = userRequestDto.Name,
            Role = parsedRole,
            Email = userRequestDto.Email,
            Phone = userRequestDto.Phone,
            Password = BCrypt.Net.BCrypt.HashPassword(userRequestDto.Password),
            Status = true
        };

        await _userRepository.UserRegisterAsync(userModel);
        return (true, null!);
    }

}
