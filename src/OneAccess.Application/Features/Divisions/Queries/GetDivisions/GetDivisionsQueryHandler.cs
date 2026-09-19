using Mapster;
using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Divisions.Queries.GetDivisions;

public class GetDivisionsQueryHandler : IRequestHandler<GetDivisionsQuery, Result<IReadOnlyList<DivisionDto>>>
{
    private readonly IReadDbContext _readDbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetDivisionsQueryHandler(
        IReadDbContext readDbContext,
        ICurrentUserService currentUserService)
    {
        _readDbContext = readDbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<DivisionDto>>> Handle(GetDivisionsQuery request, CancellationToken cancellationToken)
    {
        var isSysAdmin = await _currentUserService.IsSystemAdministratorAsync(cancellationToken);

        var query = _readDbContext.Divisions.AsQueryable();

        if (!isSysAdmin)
        {
            var assignedDivisionIds = await _currentUserService.GetAssignedDivisionIdsAsync(cancellationToken);
            query = query.Where(d => assignedDivisionIds.Contains(d.Id));
        }

        var divisions = query
            .OrderBy(d => d.Name)
            .ProjectToType<DivisionDto>()
            .ToList();

        return Result<IReadOnlyList<DivisionDto>>.Success(divisions);
    }
}
