using MediatR;
using OneAccess.Application.Common.Models;
using OneAccess.Application.Features.SubSystems.Queries.GetSubSystems;

namespace OneAccess.Application.Features.SubSystems.Queries.GetUserSubSystems;

/// <summary>
/// Query to retrieve configured per-user sub-system overrides for a given user.
/// </summary>
public record GetUserSubSystemsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<SubSystemDto>>>;
