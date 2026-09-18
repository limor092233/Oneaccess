using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Command to log out the current user and revoke their active session.
/// </summary>
public record LogoutCommand(string? RefreshToken = null) : IRequest<Result>;
