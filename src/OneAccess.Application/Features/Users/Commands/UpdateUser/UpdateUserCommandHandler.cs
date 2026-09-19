using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UpdateUserResponse>>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateUserCommandHandler(
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

    public async Task<Result<UpdateUserResponse>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
        {
            return Result<UpdateUserResponse>.NotFound("User not found.");
        }

        // Check email uniqueness if email changed
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = _readDbContext.Users
                .Any(u => u.Email.ToLower() == normalizedEmail && u.Id != user.Id);
            if (emailExists)
            {
                return Result<UpdateUserResponse>.Conflict("A user with this email address already exists.");
            }
        }

        // Guard 6: Reassignment across Divisions check
        if (request.DivisionId != user.DivisionId)
        {
            var isSysAdmin = await _currentUserService.IsSystemAdministratorAsync(cancellationToken);
            if (!isSysAdmin)
            {
                if (!request.DivisionId.HasValue)
                {
                    return Result<UpdateUserResponse>.Forbidden("Cannot remove division assignment without System Administrator privileges.");
                }

                var assignedDivisions = await _currentUserService.GetAssignedDivisionIdsAsync(cancellationToken);
                if (!assignedDivisions.Contains(request.DivisionId.Value))
                {
                    return Result<UpdateUserResponse>.Forbidden("You cannot reassign a user to a division outside your managed divisions.");
                }
            }
        }

        var now = _dateTimeProvider.UtcNow;
        user.Email = normalizedEmail;
        user.FullName = request.FullName.Trim();
        user.DivisionId = request.DivisionId;
        user.SectionId = request.SectionId;

        if (request.Status.HasValue)
        {
            user.Status = request.Status.Value;
        }

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
            Action = "user_updated",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Details = $"{{\"Email\":\"{user.Email}\",\"FullName\":\"{user.FullName}\",\"DivisionId\":\"{user.DivisionId}\",\"SectionId\":\"{user.SectionId}\",\"Status\":\"{user.Status}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new UpdateUserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.DivisionId,
            user.SectionId,
            user.Status,
            user.UpdatedAt);

        return Result<UpdateUserResponse>.Success(response);
    }
}
