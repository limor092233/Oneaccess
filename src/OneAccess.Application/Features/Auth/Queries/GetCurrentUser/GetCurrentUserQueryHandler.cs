using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IReadDbContext _readDbContext;
    private readonly ISubSystemAccessService _subSystemAccessService;

    public GetCurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        IReadDbContext readDbContext,
        ISubSystemAccessService subSystemAccessService)
    {
        _currentUserService = currentUserService;
        _readDbContext = readDbContext;
        _subSystemAccessService = subSystemAccessService;
    }

    public async Task<Result<CurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return Result<CurrentUserResponse>.Unauthorized();
        }

        var userId = _currentUserService.UserId.Value;
        var user = _readDbContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            return Result<CurrentUserResponse>.NotFound("User not found.");
        }

        var permissions = await _currentUserService.GetPermissionsAsync(cancellationToken);
        var accessibleSubsystems = await _subSystemAccessService.GetAccessibleSubSystemsAsync(userId, cancellationToken);

        var subSystemInfos = accessibleSubsystems
            .Select(s => new CurrentSubsystemInfo(s.Id, s.Code, s.Name, s.BaseUrl))
            .ToList();

        var response = new CurrentUserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.IsSystemAdministrator,
            _currentUserService.Roles,
            permissions,
            subSystemInfos);

        return Result<CurrentUserResponse>.Success(response);
    }
}
