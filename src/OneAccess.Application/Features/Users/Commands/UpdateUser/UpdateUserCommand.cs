using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Command to update an existing user account.
/// Implements IDivisionScopedRequest so DivisionScopeBehavior verifies caller scope against user's current division.
/// </summary>
public record UpdateUserCommand(
    Guid Id,
    string Email,
    string FullName,
    Guid? DivisionId = null,
    Guid? SectionId = null,
    UserStatus? Status = null
) : IRequest<Result<UpdateUserResponse>>, IDivisionScopedRequest
{
    public Task<Guid?> GetTargetDivisionIdAsync(IReadDbContext db, CancellationToken ct = default)
    {
        var user = db.Users.FirstOrDefault(u => u.Id == Id);
        return Task.FromResult(user?.DivisionId);
    }
}
