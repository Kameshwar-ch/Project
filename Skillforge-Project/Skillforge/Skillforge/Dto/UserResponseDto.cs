using System;
using Skillforge.Domain;
namespace Skillforge.Dto;
public class UserResponseDto 
{
    public int UserID { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required UserRole RoleName { get; set; }
    public required string Phone { get; set; }
    public bool Status { get; set; }
}
