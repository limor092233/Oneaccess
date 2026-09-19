using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.SubSystems.Commands.RevokeRoleSubSystemAccess;

/// <summary>
/// Handler for RevokeRoleSubSystemAccessCommand.
/// </summary>
public class RevokeRoleSubSystemAccessCommandHandler : IRequestHandler<RevokeRoleSubSystemAccessCommand, Result>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RevokeRoleSubSystemAccessCommandHandler(
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

    public async Task<Result> Handle(RevokeRoleSubSystemAccessCommand request, CancellationToken cancellationToken)
    {
        var roleRepo = _unitOfWork.Repository<Role>();
        var role = await roleRepo.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            return Result.NotFound("Role not found.");
        }

        var subSystemRepo = _unitOfWork.Repository<SubSystem>();
        var subSystem = await subSystemRepo.GetByIdAsync(request.SubSystemId, cancellationToken);
        if (subSystem == null)
        {
            return Result.NotFound("Sub-system not found.");
        }

        if (string.Equals(role.Name, SystemRoles.SystemAdministrator, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure("Sub-system access restrictions cannot be modified for the System Administrator role.", "SystemAdminSubSystemRestrictionNotAllowed", 400);
        }

        var access = _readDbContext.RoleSubSystemAccesses
            .FirstOrDefault(rsa => rsa.RoleId == request.RoleId && rsa.SubSystemId == request.SubSystemId);

        if (access != null)
        {
            var accessRepo = _unitOfWork.Repository<RoleSubSystemAccess>();
            accessRepo.Remove(access);
        }

        // Invalidate cached sub-systems for all users currently assigned to this role
        var affectedUserIds = _readDbContext.UserRoles
            .Where(ur => ur.RoleId == role.Id)
            .Select(ur => ur.UserId)
            .ToList();

        foreach (var userId in affectedUserIds)
        {
            await _cacheService.RemoveAsync($"subsystems:{userId}", cancellationToken);
        }

        var now = _dateTimeProvider.UtcNow;
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "role_subsystem_access_revoked",
            EntityType = "RoleSubSystemAccess",
            EntityId = $"{role.Id}:{subSystem.Id}",
            Details = $"{{\"RoleId\":\"{role.Id}\",\"RoleName\":\"{role.Name}\",\"SubSystemId\":\"{subSystem.Id}\",\"SubSystemCode\":\"{subSystem.Code}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
