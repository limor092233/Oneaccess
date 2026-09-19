using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.RolePermissions.Commands.AssignPermission;

public class AssignPermissionCommandHandler : IRequestHandler<AssignPermissionCommand, Result>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AssignPermissionCommandHandler(
        IReadDbContext readDbContext,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider)
    {
        _readDbContext = readDbContext;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(AssignPermissionCommand request, CancellationToken cancellationToken)
    {
        var roleRepo = _unitOfWork.Repository<Role>();
        var role = await roleRepo.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            return Result.NotFound("Role not found.");
        }

        var permRepo = _unitOfWork.Repository<Permission>();
        var permission = await permRepo.GetByIdAsync(request.PermissionId, cancellationToken);
        if (permission == null)
        {
            return Result.NotFound("Permission not found.");
        }

        // Guard 4: AssignRolePermission / RevokeRolePermission reject any modification where Role.Name == SystemRoles.SystemAdministrator
        if (string.Equals(role.Name, SystemRoles.SystemAdministrator, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure("Permissions for System Administrator role cannot be modified.", "SystemAdminPermissionsImmutable", 400);
        }

        // Guard: Non-delegable system permissions cannot be assigned to any role (OneAccess.md Section 5)
        if (!permission.IsDelegable)
        {
            return Result.Failure($"Permission '{permission.Code}' is a non-delegable system permission and cannot be assigned to roles.", "NonDelegablePermission", 400);
        }

        var exists = _readDbContext.RolePermissions
            .Any(rp => rp.RoleId == request.RoleId && rp.PermissionId == request.PermissionId);

        if (!exists)
        {
            var rolePermissionRepo = _unitOfWork.Repository<RolePermission>();
            await rolePermissionRepo.AddAsync(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            }, cancellationToken);
        }

        var now = _dateTimeProvider.UtcNow;

        // Guard 7: Invalidate cached permissions for all users currently assigned to this role
        // (Permissions are evaluated live on next request via ICurrentUserService.GetPermissionsAsync)
        var affectedUserIds = _readDbContext.UserRoles
            .Where(ur => ur.RoleId == role.Id)
            .Select(ur => ur.UserId)
            .ToList();

        foreach (var userId in affectedUserIds)
        {
            await _cacheService.RemoveAsync($"permissions:{userId}", cancellationToken);
        }

        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "role_permission_assigned",
            EntityType = "RolePermission",
            EntityId = $"{role.Id}:{permission.Id}",
            Details = $"{{\"RoleId\":\"{role.Id}\",\"RoleName\":\"{role.Name}\",\"PermissionId\":\"{permission.Id}\",\"PermissionCode\":\"{permission.Code}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
