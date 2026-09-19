using Mapster;
using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Sections.Queries.GetSectionsByDivision;

public class GetSectionsByDivisionQueryHandler : IRequestHandler<GetSectionsByDivisionQuery, Result<IReadOnlyList<SectionDto>>>
{
    private readonly IReadDbContext _readDbContext;

    public GetSectionsByDivisionQueryHandler(IReadDbContext readDbContext)
    {
        _readDbContext = readDbContext;
    }

    public Task<Result<IReadOnlyList<SectionDto>>> Handle(GetSectionsByDivisionQuery request, CancellationToken cancellationToken)
    {
        var divisionExists = _readDbContext.Divisions.Any(d => d.Id == request.DivisionId);
        if (!divisionExists)
        {
            return Task.FromResult(Result<IReadOnlyList<SectionDto>>.NotFound("Division not found."));
        }

        var sections = _readDbContext.Sections
            .Where(s => s.DivisionId == request.DivisionId)
            .OrderBy(s => s.Name)
            .ProjectToType<SectionDto>()
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<SectionDto>>.Success(sections));
    }
}
