using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.SubSystems.Commands.RevokeRoleSubSystemAccess;

/// <summary>
/// Command to remove a sub-system from a role's allow-list restriction.
/// </summary>
public record RevokeRoleSubSystemAccessCommand(Guid RoleId, Guid SubSystemId) : IRequest<Result>;
