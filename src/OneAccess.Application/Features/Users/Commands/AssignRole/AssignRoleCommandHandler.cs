using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.Users.Commands.AssignRole;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AssignRoleCommandHandler(
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

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result.NotFound("User not found.");
        }

        var roleRepo = _unitOfWork.Repository<Role>();
        var role = await roleRepo.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            return Result.NotFound("Role not found.");
        }

        // Guard 5: Only System Administrators can assign system roles (IsSystemRole == true)
        if (role.IsSystemRole)
        {
            var isSysAdmin = await _currentUserService.IsSystemAdministratorAsync(cancellationToken);
            if (!isSysAdmin)
            {
                return Result.Forbidden("Only System Administrators can assign system roles.");
            }
        }

        var now = _dateTimeProvider.UtcNow;
        var existingUserRole = _readDbContext.UserRoles
            .FirstOrDefault(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId);

        if (existingUserRole == null)
        {
            var userRoleRepo = _unitOfWork.Repository<UserRole>();
            await userRoleRepo.AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            }, cancellationToken);
        }

        if (string.Equals(role.Name, SystemRoles.SystemAdministrator, StringComparison.OrdinalIgnoreCase))
        {
            user.IsSystemAdministrator = true;
            user.UpdatedAt = now;
            userRepo.Update(user);
        }

        // Guard 7: Invalidate Redis caches in the same handler
        await _cacheService.RemoveAsync($"roles:{user.Id}", cancellationToken);
        await _cacheService.RemoveAsync($"permissions:{user.Id}", cancellationToken);
        await _cacheService.RemoveAsync($"assigned_divisions:{user.Id}", cancellationToken);

        // Revoke active sessions to refresh authorization state
        await _tokenRevocationService.RevokeAsync(user.Id, now, cancellationToken);

        // Write AuditLog
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "role_assigned",
            EntityType = "UserRole",
            EntityId = $"{user.Id}:{role.Id}",
            Details = $"{{\"UserId\":\"{user.Id}\",\"RoleId\":\"{role.Id}\",\"RoleName\":\"{role.Name}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
