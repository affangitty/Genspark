namespace BusBooking.Application.Interfaces;

/// <summary>
/// JWT Token Service for authentication
/// </summary>
public interface IJwtService
{
    string GenerateToken(Guid userId, string email, string role);
    string? ValidateToken(string token);
    (string accessToken, string refreshToken) GenerateTokenPair(Guid userId, string email, string role);
}
