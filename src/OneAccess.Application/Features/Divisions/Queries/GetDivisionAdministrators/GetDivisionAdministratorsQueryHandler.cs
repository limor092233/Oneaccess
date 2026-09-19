using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Divisions.Queries.GetDivisionAdministrators;

public class GetDivisionAdministratorsQueryHandler : IRequestHandler<GetDivisionAdministratorsQuery, Result<IReadOnlyList<DivisionAdministratorDto>>>
{
    private readonly IReadDbContext _readDbContext;

    public GetDivisionAdministratorsQueryHandler(IReadDbContext readDbContext)
    {
        _readDbContext = readDbContext;
    }

    public Task<Result<IReadOnlyList<DivisionAdministratorDto>>> Handle(GetDivisionAdministratorsQuery request, CancellationToken cancellationToken)
    {
        var divisionExists = _readDbContext.Divisions.Any(d => d.Id == request.DivisionId);
        if (!divisionExists)
        {
            return Task.FromResult(Result<IReadOnlyList<DivisionAdministratorDto>>.NotFound("Division not found."));
        }

        var admins = (from uda in _readDbContext.UserDivisionAssignments
                      join u in _readDbContext.Users on uda.UserId equals u.Id
                      where uda.DivisionId == request.DivisionId
                      orderby u.Username
                      select new DivisionAdministratorDto(
                          u.Id,
                          u.Username,
                          u.FullName,
                          u.Email,
                          uda.GrantedAt
                      )).ToList();

        return Task.FromResult(Result<IReadOnlyList<DivisionAdministratorDto>>.Success(admins));
    }
}
