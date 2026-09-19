using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Divisions.Queries.GetDivisionAdministrators;

public record GetDivisionAdministratorsQuery(Guid DivisionId) : IRequest<Result<IReadOnlyList<DivisionAdministratorDto>>>, IDivisionScopedRequest
{
    public Task<Guid?> GetTargetDivisionIdAsync(IReadDbContext db, CancellationToken ct = default)
    {
        return Task.FromResult<Guid?>(DivisionId);
    }
}
