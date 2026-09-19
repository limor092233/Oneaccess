using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.Roles.Commands.DeleteRole;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteRoleCommandHandler(
        IReadDbContext readDbContext,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _readDbContext = readDbContext;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var roleRepo = _unitOfWork.Repository<Role>();
        var role = await roleRepo.GetByIdAsync(request.Id, cancellationToken);
        if (role == null)
        {
            return Result.NotFound("Role not found.");
        }

        // Guard 3a: DeleteRole rejects deleting any Role where IsSystemRole == true
        if (role.IsSystemRole)
        {
            return Result.Conflict("Cannot delete built-in system role.");
        }

        // Guard 3b: DeleteRole rejects deleting any Role where any User still holds it
        var isAssignedToUsers = _readDbContext.UserRoles
            .Any(ur => ur.RoleId == role.Id);
        if (isAssignedToUsers)
        {
            return Result.Conflict("Cannot delete role assigned to one or more users.");
        }

        roleRepo.Remove(role);

        var now = _dateTimeProvider.UtcNow;
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "role_deleted",
            EntityType = "Role",
            EntityId = role.Id.ToString(),
            Details = $"{{\"Name\":\"{role.Name}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
