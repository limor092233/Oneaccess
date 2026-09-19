using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Application.Features.SubSystems.Queries.GetSubSystems;

namespace OneAccess.Application.Features.SubSystems.Queries.GetMySubSystems;

/// <summary>
/// Handler for GetMySubSystemsQuery.
/// </summary>
public class GetMySubSystemsQueryHandler : IRequestHandler<GetMySubSystemsQuery, Result<IReadOnlyList<SubSystemDto>>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubSystemAccessService _subSystemAccessService;

    public GetMySubSystemsQueryHandler(
        ICurrentUserService currentUserService,
        ISubSystemAccessService subSystemAccessService)
    {
        _currentUserService = currentUserService;
        _subSystemAccessService = subSystemAccessService;
    }

    public async Task<Result<IReadOnlyList<SubSystemDto>>> Handle(GetMySubSystemsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<IReadOnlyList<SubSystemDto>>.Unauthorized("User is not authenticated.");
        }

        var accessible = await _subSystemAccessService.GetAccessibleSubSystemsAsync(userId.Value, cancellationToken);
        var dtos = accessible.Select(s => new SubSystemDto(
            s.Id,
            s.Code,
            s.Name,
            s.BaseUrl,
            s.Audience,
            s.IsActive
        )).ToList();

        return Result<IReadOnlyList<SubSystemDto>>.Success(dtos);
    }
}
