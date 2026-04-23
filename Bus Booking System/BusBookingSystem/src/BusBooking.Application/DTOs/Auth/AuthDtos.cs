namespace BusBooking.Application.DTOs.Auth;

public class LoginRequestDto
{
    /// <summary>Email, phone, or username (preferred for user login).</summary>
    public string? Identifier { get; set; }

    /// <summary>Legacy field — same as identifier when set.</summary>
    public string? Email { get; set; }

    public string Password { get; set; } = string.Empty;

    public string ResolveLoginId()
    {
        var a = Identifier?.Trim();
        if (!string.IsNullOrEmpty(a))
            return a;
        return Email?.Trim() ?? string.Empty;
    }
}

public class RegisterRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    /// <summary>Optional unique login handle.</summary>
    public string? UserName { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class OperatorRegistrationRequestDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class UpdateProfileRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class UpdateOperatorProfileRequestDto
{
    public string ContactPersonName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class AddOperatorLocationRequestDto
{
    public string City { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string Landmark { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PinCode { get; set; } = string.Empty;
}
