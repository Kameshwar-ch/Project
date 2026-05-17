using System;
using FluentValidation;
using Skillforge.Dto;

public class RegisterValidator : AbstractValidator<UserRequestDto>
{
	public RegisterValidator()
	{

		RuleFor(x => x.Name)
			.MinimumLength(3).WithMessage("Username must be at least 3 characters.")
			.MaximumLength(50).WithMessage("Username cannot exceed 50 characters.")
			.Matches("^[a-zA-Z ]+$").WithMessage("Name can only contain letters and spaces.");

		RuleFor(x => x.Email)
		.Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("Please provide a valid email address.");

		RuleFor(x => x.Password)
			.MinimumLength(8).WithMessage("Password must be at least 8 characters.")
			.Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
			.Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
			.Matches("[0-9]").WithMessage("Password must contain at least one digit.")
			.Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");


		RuleFor(x => x.Phone)
			.Matches(@"^\+?[1-9]\d{9,14}$")
			.WithMessage("Please provide a valid phone number.");

	}
}
