using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Sections.Commands.CreateSection;

public record CreateSectionCommand(Guid DivisionId, string Name, string Description) : IRequest<Result<CreateSectionResponse>>, IDivisionScopedRequest
{
    public Task<Guid?> GetTargetDivisionIdAsync(IReadDbContext db, CancellationToken ct = default)
    {
        return Task.FromResult<Guid?>(DivisionId);
    }
}
