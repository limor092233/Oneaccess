using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.RolePermissions.Commands.RevokePermission;

/// <summary>
/// Command to revoke a permission from a role.
/// </summary>
public record RevokePermissionCommand(Guid RoleId, Guid PermissionId) : IRequest<Result>;
