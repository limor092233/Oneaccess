using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Domain.Entities;
using OneAccess.Infrastructure.Options;

namespace OneAccess.Infrastructure.Identity;

/// <summary>
/// Service generating RS256 signed JWT access tokens and hashed refresh tokens.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly RsaKeyProvider _rsaKeyProvider;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtTokenService(
        IOptions<JwtOptions> jwtOptions,
        RsaKeyProvider rsaKeyProvider,
        IDateTimeProvider dateTimeProvider)
    {
        _jwtOptions = jwtOptions.Value;
        _rsaKeyProvider = rsaKeyProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles, string? audience = null)
    {
        var (kid, signingKey) = _rsaKeyProvider.GetActiveSigningKey();
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        var now = _dateTimeProvider.UtcNow;
        var expires = now.AddMinutes(_jwtOptions.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        var header = new JwtHeader(credentials)
        {
            { "kid", kid }
        };

        var payload = new JwtPayload(_jwtOptions.Issuer, audience ?? _jwtOptions.Audience, claims, now, expires, now);
        var token = new JwtSecurityToken(header, payload);

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(token);
    }

    public (string Token, string Hash) GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var tokenString = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var tokenHash = HashToken(tokenString);
        return (tokenString, tokenHash);
    }

    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
