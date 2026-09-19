using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Application.Features.SubSystems.Queries.GetSubSystems;

namespace OneAccess.Application.Features.SubSystems.Queries.GetUserSubSystems;

/// <summary>
/// Handler for GetUserSubSystemsQuery.
/// </summary>
public class GetUserSubSystemsQueryHandler : IRequestHandler<GetUserSubSystemsQuery, Result<IReadOnlyList<SubSystemDto>>>
{
    private readonly IReadDbContext _readDbContext;

    public GetUserSubSystemsQueryHandler(IReadDbContext readDbContext)
    {
        _readDbContext = readDbContext;
    }

    public Task<Result<IReadOnlyList<SubSystemDto>>> Handle(GetUserSubSystemsQuery request, CancellationToken cancellationToken)
    {
        var userExists = _readDbContext.Users.Any(u => u.Id == request.UserId);
        if (!userExists)
        {
            return Task.FromResult(Result<IReadOnlyList<SubSystemDto>>.NotFound("User not found."));
        }

        var subSystems = (from usa in _readDbContext.UserSubSystemAccesses
                          join s in _readDbContext.SubSystems on usa.SubSystemId equals s.Id
                          where usa.UserId == request.UserId
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
