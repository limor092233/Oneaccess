using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Users.Queries.GetUserById;

/// <summary>
/// Query to retrieve detailed information for a specific user.
/// Implements IDivisionScopedRequest so DivisionScopeBehavior verifies caller scope against user's division.
/// </summary>
public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDetailsDto>>, IDivisionScopedRequest
{
    public Task<Guid?> GetTargetDivisionIdAsync(IReadDbContext db, CancellationToken ct = default)
    {
        var user = db.Users.FirstOrDefault(u => u.Id == Id);
        return Task.FromResult(user?.DivisionId);
    }
}
