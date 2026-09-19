using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Sections.Commands.UpdateSection;

public record UpdateSectionCommand(Guid Id, string Name, string Description) : IRequest<Result>, IDivisionScopedRequest
{
    public Task<Guid?> GetTargetDivisionIdAsync(IReadDbContext db, CancellationToken ct = default)
    {
        var divisionId = db.Sections
            .Where(s => s.Id == Id)
            .Select(s => (Guid?)s.DivisionId)
            .FirstOrDefault();

        return Task.FromResult(divisionId);
    }
}
