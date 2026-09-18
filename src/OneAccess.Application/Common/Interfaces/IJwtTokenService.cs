using OneAccess.Domain.Entities;

namespace OneAccess.Application.Common.Interfaces;

/// <summary>
/// Service contract for generating and validating RS256 JWT access tokens and refresh tokens.
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, string? audience = null);
    (string Token, string Hash) GenerateRefreshToken();
    string HashToken(string token);
}
