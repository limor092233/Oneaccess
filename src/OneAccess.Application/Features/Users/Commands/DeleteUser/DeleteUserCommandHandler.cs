using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteUserCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        ITokenRevocationService tokenRevocationService,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _tokenRevocationService = tokenRevocationService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
        {
            return Result.NotFound("User not found.");
        }

        var now = _dateTimeProvider.UtcNow;

        // Guard 1: Soft delete (Status change), never hard delete
        user.Status = UserStatus.Inactive;
        user.UpdatedAt = now;
        userRepo.Update(user);

        // Guard 7: Invalidate Redis caches in the same handler
        await _cacheService.RemoveAsync($"roles:{user.Id}", cancellationToken);
        await _cacheService.RemoveAsync($"permissions:{user.Id}", cancellationToken);
        await _cacheService.RemoveAsync($"assigned_divisions:{user.Id}", cancellationToken);

        // Revoke active sessions
        await _tokenRevocationService.RevokeAsync(user.Id, now, cancellationToken);

        // Write AuditLog
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "user_deleted",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Details = $"{{\"Username\":\"{user.Username}\",\"Status\":\"{user.Status}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
