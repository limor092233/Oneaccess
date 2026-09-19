using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.SubSystems.Commands.RevokeUserSubSystemAccess;

/// <summary>
/// Handler for RevokeUserSubSystemAccessCommand.
/// </summary>
public class RevokeUserSubSystemAccessCommandHandler : IRequestHandler<RevokeUserSubSystemAccessCommand, Result>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RevokeUserSubSystemAccessCommandHandler(
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

    public async Task<Result> Handle(RevokeUserSubSystemAccessCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result.NotFound("User not found.");
        }

        var subSystemRepo = _unitOfWork.Repository<SubSystem>();
        var subSystem = await subSystemRepo.GetByIdAsync(request.SubSystemId, cancellationToken);
        if (subSystem == null)
        {
            return Result.NotFound("Sub-system not found.");
        }

        var access = _readDbContext.UserSubSystemAccesses
            .FirstOrDefault(usa => usa.UserId == request.UserId && usa.SubSystemId == request.SubSystemId);

        if (access != null)
        {
            var accessRepo = _unitOfWork.Repository<UserSubSystemAccess>();
            accessRepo.Remove(access);
        }

        // Invalidate cached sub-systems for this user
        await _cacheService.RemoveAsync($"subsystems:{user.Id}", cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "user_subsystem_access_revoked",
            EntityType = "UserSubSystemAccess",
            EntityId = $"{user.Id}:{subSystem.Id}",
            Details = $"{{\"UserId\":\"{user.Id}\",\"Username\":\"{user.Username}\",\"SubSystemId\":\"{subSystem.Id}\",\"SubSystemCode\":\"{subSystem.Code}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
