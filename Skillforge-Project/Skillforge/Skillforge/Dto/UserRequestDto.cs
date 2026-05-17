using System;
namespace Skillforge.Dto;
using System.ComponentModel.DataAnnotations;

public class UserRequestDto
{
	[Required]
	public string? Name { get; set; }

	[Required]

	public string? Email { get; set; }

	[Required]
	public string? Role {get;set;}

	[Required]
	public string? Phone { get; set; }

	[Required]
	public string? Password { get; set; }
}
