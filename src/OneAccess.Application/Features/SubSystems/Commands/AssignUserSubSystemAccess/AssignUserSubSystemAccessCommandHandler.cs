using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.SubSystems.Commands.AssignUserSubSystemAccess;

/// <summary>
/// Handler for AssignUserSubSystemAccessCommand.
/// </summary>
public class AssignUserSubSystemAccessCommandHandler : IRequestHandler<AssignUserSubSystemAccessCommand, Result>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AssignUserSubSystemAccessCommandHandler(
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

    public async Task<Result> Handle(AssignUserSubSystemAccessCommand request, CancellationToken cancellationToken)
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

        var exists = _readDbContext.UserSubSystemAccesses
            .Any(usa => usa.UserId == request.UserId && usa.SubSystemId == request.SubSystemId);

        var now = _dateTimeProvider.UtcNow;
        if (!exists)
        {
            var accessRepo = _unitOfWork.Repository<UserSubSystemAccess>();
            await accessRepo.AddAsync(new UserSubSystemAccess
            {
                UserId = user.Id,
                SubSystemId = subSystem.Id,
                GrantedAt = now
            }, cancellationToken);
        }

        // Invalidate cached sub-systems for this user
        await _cacheService.RemoveAsync($"subsystems:{user.Id}", cancellationToken);

        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "user_subsystem_access_assigned",
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
