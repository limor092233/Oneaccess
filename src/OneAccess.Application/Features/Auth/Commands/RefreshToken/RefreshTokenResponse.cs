namespace OneAccess.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInMinutes);
