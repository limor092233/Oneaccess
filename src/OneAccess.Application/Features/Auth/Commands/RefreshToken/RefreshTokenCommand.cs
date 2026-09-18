using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Command to rotate a refresh token and issue a new access token.
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken,
    string? SubSystemCode = null) : IRequest<Result<RefreshTokenResponse>>;
