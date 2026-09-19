using MediatR;
using OneAccess.Application.Common.Models;
using OneAccess.Application.Features.SubSystems.Queries.GetSubSystems;

namespace OneAccess.Application.Features.SubSystems.Queries.GetMySubSystems;

/// <summary>
/// Query to retrieve the effective active sub-systems accessible by the current authenticated user.
/// </summary>
public record GetMySubSystemsQuery : IRequest<Result<IReadOnlyList<SubSystemDto>>>;
