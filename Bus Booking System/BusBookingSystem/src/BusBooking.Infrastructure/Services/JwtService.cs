using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusBooking.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BusBooking.Infrastructure.Services;

public class JwtService : IJwtService
{
    private const string TokenUseClaim = "token_use";
    private const string RefreshValue = "refresh";

    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;
    private readonly int _refreshExpiryDays;

    public JwtService(IConfiguration configuration)
    {
        _secretKey = configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
        _issuer = configuration["JwtSettings:Issuer"] ?? "BusBookingAPI";
        _audience = configuration["JwtSettings:Audience"] ?? "BusBookingClient";
        _expiryMinutes = int.Parse(configuration["JwtSettings:ExpiryMinutes"] ?? "60");
        _refreshExpiryDays = int.Parse(configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
    }

    public string GenerateToken(Guid userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(TokenUseClaim, "access"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string? ValidateToken(string token)
    {
        try
        {
            var principal = ReadValidatedPrincipal(token, requireAccess: true);
            return principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        }
        catch
        {
            return null;
        }
    }

    public (string accessToken, string refreshToken) GenerateTokenPair(
        Guid userId, string email, string role)
    {
        var accessToken = GenerateToken(userId, email, role);
        var refreshToken = GenerateRefreshToken(userId, email, role);
        return (accessToken, refreshToken);
    }

    public (Guid userId, string email, string role)? ParseRefreshToken(string refreshToken)
    {
        try
        {
            var principal = ReadValidatedPrincipal(refreshToken, requireAccess: false);
            if (principal == null)
                return null;

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            if (sub == null || email == null || role == null || !Guid.TryParse(sub, out var userId))
                return null;

            return (userId, email, role);
        }
        catch
        {
            return null;
        }
    }

    private ClaimsPrincipal? ReadValidatedPrincipal(string token, bool requireAccess)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var handler = new JwtSecurityTokenHandler();

        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        }, out var validatedToken);

        var jwt = (JwtSecurityToken)validatedToken;
        var use = jwt.Claims.FirstOrDefault(c => c.Type == TokenUseClaim)?.Value;
        if (requireAccess)
        {
            if (use == RefreshValue)
                return null;
        }
        else if (use != RefreshValue)
            return null;

        return principal;
    }

    private string GenerateRefreshToken(Guid userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(TokenUseClaim, RefreshValue),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_refreshExpiryDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
