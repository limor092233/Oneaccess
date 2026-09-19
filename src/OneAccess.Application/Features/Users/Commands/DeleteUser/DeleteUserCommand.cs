using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Command to soft-delete / deactivate a user account.
/// Implements IDivisionScopedRequest so DivisionScopeBehavior verifies caller scope against user's current division.
/// </summary>
public record DeleteUserCommand(Guid Id) : IRequest<Result>, IDivisionScopedRequest
{
    public Task<Guid?> GetTargetDivisionIdAsync(IReadDbContext db, CancellationToken ct = default)
    {
        var user = db.Users.FirstOrDefault(u => u.Id == Id);
        return Task.FromResult(user?.DivisionId);
    }
}
