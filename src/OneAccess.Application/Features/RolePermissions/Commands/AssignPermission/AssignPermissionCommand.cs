using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.RolePermissions.Commands.AssignPermission;

/// <summary>
/// Command to assign a permission to a role.
/// </summary>
public record AssignPermissionCommand(Guid RoleId, Guid PermissionId) : IRequest<Result>;
