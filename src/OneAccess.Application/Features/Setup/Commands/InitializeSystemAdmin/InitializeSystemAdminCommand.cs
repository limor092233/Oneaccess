using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Setup.Commands.InitializeSystemAdmin;

/// <summary>
/// Command to complete first-run setup by initializing the root System Administrator.
/// </summary>
public record InitializeSystemAdminCommand(
    string Code,
    string Username,
    string Email,
    string Password,
    string FullName) : IRequest<Result<InitializeSystemAdminResponse>>;
