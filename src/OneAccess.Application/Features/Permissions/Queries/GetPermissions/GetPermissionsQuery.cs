using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Permissions.Queries.GetPermissions;

/// <summary>
/// Query to list all seeded system permissions.
/// Used to populate role-permission assignment UI (OneAccess.md Section 14).
/// </summary>
public record GetPermissionsQuery() : IRequest<Result<IReadOnlyList<PermissionDto>>>;
