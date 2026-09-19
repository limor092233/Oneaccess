using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Application.Features.SubSystems.Queries.GetSubSystems;

namespace OneAccess.Application.Features.SubSystems.Queries.GetRoleSubSystems;

/// <summary>
/// Handler for GetRoleSubSystemsQuery.
/// </summary>
public class GetRoleSubSystemsQueryHandler : IRequestHandler<GetRoleSubSystemsQuery, Result<IReadOnlyList<SubSystemDto>>>
{
    private readonly IReadDbContext _readDbContext;

    public GetRoleSubSystemsQueryHandler(IReadDbContext readDbContext)
    {
        _readDbContext = readDbContext;
    }

    public Task<Result<IReadOnlyList<SubSystemDto>>> Handle(GetRoleSubSystemsQuery request, CancellationToken cancellationToken)
    {
        var roleExists = _readDbContext.Roles.Any(r => r.Id == request.RoleId);
        if (!roleExists)
        {
            return Task.FromResult(Result<IReadOnlyList<SubSystemDto>>.NotFound("Role not found."));
        }

        var subSystems = (from rsa in _readDbContext.RoleSubSystemAccesses
                          join s in _readDbContext.SubSystems on rsa.SubSystemId equals s.Id
                          where rsa.RoleId == request.RoleId
                          orderby s.Code
                          select new SubSystemDto(
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
