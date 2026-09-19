using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.SubSystems.Queries.GetSubSystems;

/// <summary>
/// Handler for GetSubSystemsQuery.
/// </summary>
public class GetSubSystemsQueryHandler : IRequestHandler<GetSubSystemsQuery, Result<IReadOnlyList<SubSystemDto>>>
{
    private readonly IReadDbContext _readDbContext;

    public GetSubSystemsQueryHandler(IReadDbContext readDbContext)
    {
        _readDbContext = readDbContext;
    }

    public Task<Result<IReadOnlyList<SubSystemDto>>> Handle(GetSubSystemsQuery request, CancellationToken cancellationToken)
    {
        var subSystems = _readDbContext.SubSystems
            .OrderBy(s => s.Code)
            .Select(s => new SubSystemDto(
                s.Id,
                s.Code,
                s.Name,
                s.BaseUrl,
                s.Audience,
                s.IsActive
            ))
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<SubSystemDto>>.Success(subSystems));
    }
}
