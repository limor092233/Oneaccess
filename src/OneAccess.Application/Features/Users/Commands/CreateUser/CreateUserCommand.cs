using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Enums;
using MediatR;

namespace OneAccess.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Command to create a new user account.
/// Implements IDivisionScopedRequest so DivisionScopeBehavior verifies caller scope if non-SysAdmin.
/// </summary>
public record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    string FullName,
    Guid? DivisionId = null,
    Guid? SectionId = null,
    IReadOnlyList<Guid>? RoleIds = null
) : IRequest<Result<CreateUserResponse>>, IDivisionScopedRequest
{
    public Task<Guid?> GetTargetDivisionIdAsync(IReadDbContext db, CancellationToken ct = default)
    {
        return Task.FromResult(DivisionId);
    }
}
