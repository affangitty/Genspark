using System.ComponentModel.DataAnnotations;
using FluentValidation;
using BusBooking.Application.DTOs.Auth;

namespace BusBooking.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.ResolveLoginId()))
            .WithMessage("Email, phone, or username is required");

        RuleFor(x => x)
            .Must(x =>
            {
                var id = x.ResolveLoginId();
                return !id.Contains('@', StringComparison.Ordinal) ||
                       new EmailAddressAttribute().IsValid(id);
            })
            .WithMessage("Invalid email format");

        RuleFor(x => x)
            .Must(x =>
            {
                var id = x.ResolveLoginId();
                if (id.Contains('@', StringComparison.Ordinal))
                    return true;
                return id.Length >= 3;
            })
            .WithMessage("Login must be at least 3 characters");
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(150).WithMessage("Full name cannot exceed 150 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain a lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain a digit");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Invalid phone number format");

        When(x => !string.IsNullOrWhiteSpace(x.UserName), () =>
        {
            RuleFor(x => x.UserName!)
                .MinimumLength(3).WithMessage("Username must be at least 3 characters")
                .MaximumLength(50).WithMessage("Username cannot exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username may only contain letters, numbers, and underscores");
        });
    }
}

public class OperatorRegistrationValidator : AbstractValidator<OperatorRegistrationRequestDto>
{
    public OperatorRegistrationValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(200);

        RuleFor(x => x.ContactPersonName)
            .NotEmpty().WithMessage("Contact person name is required")
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Must contain uppercase letter")
            .Matches(@"[a-z]").WithMessage("Must contain lowercase letter")
            .Matches(@"[0-9]").WithMessage("Must contain a digit");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Invalid phone number format");
    }
}