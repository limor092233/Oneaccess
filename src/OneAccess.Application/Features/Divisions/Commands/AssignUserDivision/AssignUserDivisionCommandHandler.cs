using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.Divisions.Commands.AssignUserDivision;

public class AssignUserDivisionCommandHandler : IRequestHandler<AssignUserDivisionCommand, Result>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AssignUserDivisionCommandHandler(
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

    public async Task<Result> Handle(AssignUserDivisionCommand request, CancellationToken cancellationToken)
    {
        var divisionRepo = _unitOfWork.Repository<Division>();
        var division = await divisionRepo.GetByIdAsync(request.DivisionId, cancellationToken);
        if (division == null)
        {
            return Result.NotFound("Division not found.");
        }

        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result.NotFound("User not found.");
        }

        var exists = _readDbContext.UserDivisionAssignments
            .Any(uda => uda.DivisionId == request.DivisionId && uda.UserId == request.UserId);

        var now = _dateTimeProvider.UtcNow;

        if (!exists)
        {
            var assignmentRepo = _unitOfWork.Repository<UserDivisionAssignment>();
            await assignmentRepo.AddAsync(new UserDivisionAssignment
            {
                DivisionId = division.Id,
                UserId = user.Id,
                GrantedAt = now
            }, cancellationToken);
        }

        // Invalidate cached assigned divisions for this user
        await _cacheService.RemoveAsync($"assigned_divisions:{user.Id}", cancellationToken);

        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "user_division_assigned",
            EntityType = "UserDivisionAssignment",
            EntityId = $"{division.Id}:{user.Id}",
            Details = $"{{\"DivisionId\":\"{division.Id}\",\"DivisionName\":\"{division.Name}\",\"UserId\":\"{user.Id}\",\"Username\":\"{user.Username}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
