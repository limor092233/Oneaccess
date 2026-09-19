using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Roles.Commands.CreateRole;

/// <summary>
/// Command to create a new security role.
/// </summary>
public record CreateRoleCommand(string Name, string Description) : IRequest<Result<CreateRoleResponse>>;
