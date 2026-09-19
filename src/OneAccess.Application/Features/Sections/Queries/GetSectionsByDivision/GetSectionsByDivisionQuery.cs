using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Sections.Queries.GetSectionsByDivision;

public record GetSectionsByDivisionQuery(Guid DivisionId) : IRequest<Result<IReadOnlyList<SectionDto>>>, IDivisionScopedRequest
{
    public Task<Guid?> GetTargetDivisionIdAsync(IReadDbContext db, CancellationToken ct = default)
    {
        return Task.FromResult<Guid?>(DivisionId);
    }
}
