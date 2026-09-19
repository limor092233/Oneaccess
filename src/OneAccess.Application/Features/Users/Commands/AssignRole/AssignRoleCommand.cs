using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Users.Commands.AssignRole;

/// <summary>
/// Command to assign a role to a user.
/// Implements IDivisionScopedRequest so DivisionScopeBehavior verifies caller scope against target user's division.
/// </summary>
public record AssignRoleCommand(Guid UserId, Guid RoleId) : IRequest<Result>, IDivisionScopedRequest
{
    public Task<Guid?> GetTargetDivisionIdAsync(IReadDbContext db, CancellationToken ct = default)
    {
        var user = db.Users.FirstOrDefault(u => u.Id == UserId);
        return Task.FromResult(user?.DivisionId);
    }
}
