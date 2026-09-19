using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Roles.Queries.GetRoles;

/// <summary>
/// Query to list all configured roles.
/// </summary>
public record GetRolesQuery() : IRequest<Result<IReadOnlyList<RoleDto>>>;
