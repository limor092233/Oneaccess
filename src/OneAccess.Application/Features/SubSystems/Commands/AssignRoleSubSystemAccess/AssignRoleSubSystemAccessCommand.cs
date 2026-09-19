using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.SubSystems.Commands.AssignRoleSubSystemAccess;

/// <summary>
/// Command to add a sub-system to a role's allow-list restriction.
/// </summary>
public record AssignRoleSubSystemAccessCommand(Guid RoleId, Guid SubSystemId) : IRequest<Result>;
