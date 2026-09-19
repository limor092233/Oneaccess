using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Divisions.Queries.GetDivisions;

public record GetDivisionsQuery : IRequest<Result<IReadOnlyList<DivisionDto>>>;
