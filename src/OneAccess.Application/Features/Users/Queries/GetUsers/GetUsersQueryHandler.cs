using Mapster;
using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PagedResult<UserDto>>>
{
    private readonly IReadDbContext _readDbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetUsersQueryHandler(IReadDbContext readDbContext, ICurrentUserService currentUserService)
    {
        _readDbContext = readDbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _readDbContext.Users;

        // Section 5: Filter by assigned divisions for non-System Administrators
        var isSysAdmin = await _currentUserService.IsSystemAdministratorAsync(cancellationToken);
        if (!isSysAdmin)
        {
            var assignedDivisions = await _currentUserService.GetAssignedDivisionIdsAsync(cancellationToken);
            query = query.Where(u => u.DivisionId.HasValue && assignedDivisions.Contains(u.DivisionId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(search)
                                  || u.Email.ToLower().Contains(search)
                                  || u.FullName.ToLower().Contains(search));
        }

        var totalCount = query.Count();
        var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        var items = query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ProjectToType<UserDto>()
            .ToList();

        var pagedResult = new PagedResult<UserDto>(items, totalCount, pageNumber, pageSize);
        return Result<PagedResult<UserDto>>.Success(pagedResult);
    }
}
