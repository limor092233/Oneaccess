using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.SubSystems.Commands.UpdateSubSystem;

/// <summary>
/// Handler for UpdateSubSystemCommand.
/// </summary>
public class UpdateSubSystemCommandHandler : IRequestHandler<UpdateSubSystemCommand, Result<UpdateSubSystemResponse>>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateSubSystemCommandHandler(
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

    public async Task<Result<UpdateSubSystemResponse>> Handle(UpdateSubSystemCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<SubSystem>();
        var subSystem = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (subSystem == null)
        {
            return Result<UpdateSubSystemResponse>.NotFound("Sub-system not found.");
        }

        var duplicateAudience = _readDbContext.SubSystems.Any(s => s.Audience == request.Audience && s.Id != request.Id);
        if (duplicateAudience)
        {
            return Result<UpdateSubSystemResponse>.Failure($"Another sub-system with audience '{request.Audience}' already exists.", "DuplicateSubSystemAudience", 409);
        }

        var now = _dateTimeProvider.UtcNow;
        subSystem.Name = request.Name;
        subSystem.BaseUrl = request.BaseUrl;
        subSystem.Audience = request.Audience;
        subSystem.IsActive = request.IsActive;

        repo.Update(subSystem);

        // Invalidate cached sub-systems for affected users
        var userOverrideIds = _readDbContext.UserSubSystemAccesses
            .Where(usa => usa.SubSystemId == subSystem.Id)
            .Select(usa => usa.UserId)
            .ToList();

        var roleIdsWithAccess = _readDbContext.RoleSubSystemAccesses
            .Where(rsa => rsa.SubSystemId == subSystem.Id)
            .Select(rsa => rsa.RoleId)
            .ToList();

        var usersWithRoleIds = _readDbContext.UserRoles
            .Where(ur => roleIdsWithAccess.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .ToList();

        var affectedUserIds = userOverrideIds.Concat(usersWithRoleIds).Distinct().ToList();

        foreach (var userId in affectedUserIds)
        {
            await _cacheService.RemoveAsync($"subsystems:{userId}", cancellationToken);
        }

        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "subsystem_updated",
            EntityType = "SubSystem",
            EntityId = subSystem.Id.ToString(),
            Details = $"{{\"Code\":\"{subSystem.Code}\",\"Name\":\"{subSystem.Name}\",\"Audience\":\"{subSystem.Audience}\",\"IsActive\":{subSystem.IsActive.ToString().ToLowerInvariant()}}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new UpdateSubSystemResponse(
            subSystem.Id,
            subSystem.Code,
            subSystem.Name,
            subSystem.BaseUrl,
            subSystem.Audience,
            subSystem.IsActive
        );

        return Result<UpdateSubSystemResponse>.Success(response);
    }
}
