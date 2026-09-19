using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.RolePermissions.Commands.RevokePermission;

public class RevokePermissionCommandHandler : IRequestHandler<RevokePermissionCommand, Result>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RevokePermissionCommandHandler(
        IReadDbContext readDbContext,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        ITokenRevocationService tokenRevocationService,
        IDateTimeProvider dateTimeProvider)
    {
        _readDbContext = readDbContext;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _tokenRevocationService = tokenRevocationService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RevokePermissionCommand request, CancellationToken cancellationToken)
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

        var rolePermission = _readDbContext.RolePermissions
            .FirstOrDefault(rp => rp.RoleId == request.RoleId && rp.PermissionId == request.PermissionId);

        if (rolePermission != null)
        {
            var rolePermissionRepo = _unitOfWork.Repository<RolePermission>();
            rolePermissionRepo.Remove(rolePermission);
        }

        var now = _dateTimeProvider.UtcNow;

        // Guard 7: Invalidate cached permissions for all users currently assigned to this role
        var affectedUserIds = _readDbContext.UserRoles
            .Where(ur => ur.RoleId == role.Id)
            .Select(ur => ur.UserId)
            .ToList();

        foreach (var userId in affectedUserIds)
        {
            await _cacheService.RemoveAsync($"permissions:{userId}", cancellationToken);
            await _tokenRevocationService.RevokeAsync(userId, now, cancellationToken);
        }

        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "role_permission_revoked",
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
