using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.SubSystems.Queries.GetSubSystems;

/// <summary>
/// Query to retrieve all registered sub-systems (admin management list, including inactive ones).
/// </summary>
public record GetSubSystemsQuery : IRequest<Result<IReadOnlyList<SubSystemDto>>>;
