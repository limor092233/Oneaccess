using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<UpdateRoleResponse>>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateRoleCommandHandler(
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

    public async Task<Result<UpdateRoleResponse>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var roleRepo = _unitOfWork.Repository<Role>();
        var role = await roleRepo.GetByIdAsync(request.Id, cancellationToken);
        if (role == null)
        {
            return Result<UpdateRoleResponse>.NotFound("Role not found.");
        }

        var normalizedName = request.Name.Trim();

        // Guard 2: UpdateRole rejects renaming any Role where IsSystemRole == true
        if (role.IsSystemRole && !string.Equals(role.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
        {
            return Result<UpdateRoleResponse>.Failure("Cannot rename built-in system role.", "SystemRoleRenamingNotAllowed", 400);
        }

        if (!string.Equals(role.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
        {
            var exists = _readDbContext.Roles
                .Any(r => r.Name.ToLower() == normalizedName.ToLower() && r.Id != role.Id);
            if (exists)
            {
                return Result<UpdateRoleResponse>.Conflict("A role with this name already exists.");
            }
            role.Name = normalizedName;
        }

        role.Description = request.Description?.Trim() ?? string.Empty;
        roleRepo.Update(role);

        // Guard 7: Invalidate Redis caches for all users currently assigned to this role
        var affectedUserIds = _readDbContext.UserRoles
            .Where(ur => ur.RoleId == role.Id)
            .Select(ur => ur.UserId)
            .ToList();

        foreach (var userId in affectedUserIds)
        {
            await _cacheService.RemoveAsync($"roles:{userId}", cancellationToken);
            await _cacheService.RemoveAsync($"permissions:{userId}", cancellationToken);
        }

        var now = _dateTimeProvider.UtcNow;
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "role_updated",
            EntityType = "Role",
            EntityId = role.Id.ToString(),
            Details = $"{{\"Name\":\"{role.Name}\",\"Description\":\"{role.Description}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UpdateRoleResponse>.Success(new UpdateRoleResponse(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole));
    }
}
