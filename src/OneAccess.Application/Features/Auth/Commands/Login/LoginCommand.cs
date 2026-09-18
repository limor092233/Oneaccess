using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Auth.Commands.Login;

/// <summary>
/// Command to authenticate a user with username and password.
/// </summary>
public record LoginCommand(
    string Username,
    string Password,
    string? SubSystemCode = null) : IRequest<Result<LoginResponse>>;
