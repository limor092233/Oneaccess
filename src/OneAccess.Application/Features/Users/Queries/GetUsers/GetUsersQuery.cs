using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Query to list users with optional search and pagination.
/// Automatically filters by assigned divisions for non-System Administrators (OneAccess.md Section 5).
/// </summary>
public record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null
) : IRequest<Result<PagedResult<UserDto>>>;
