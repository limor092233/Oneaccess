using MediatR;
using OneAccess.Application.Common.Models;
using OneAccess.Application.Features.SubSystems.Queries.GetSubSystems;

namespace OneAccess.Application.Features.SubSystems.Queries.GetRoleSubSystems;

/// <summary>
/// Query to retrieve configured sub-system restrictions for a given role.
/// </summary>
public record GetRoleSubSystemsQuery(Guid RoleId) : IRequest<Result<IReadOnlyList<SubSystemDto>>>;
