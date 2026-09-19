using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.Divisions.Commands.DeleteDivision;

public class DeleteDivisionCommandHandler : IRequestHandler<DeleteDivisionCommand, Result>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteDivisionCommandHandler(
        IReadDbContext readDbContext,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _readDbContext = readDbContext;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteDivisionCommand request, CancellationToken cancellationToken)
    {
        var divisionRepo = _unitOfWork.Repository<Division>();
        var division = await divisionRepo.GetByIdAsync(request.Id, cancellationToken);
        if (division == null)
        {
            return Result.NotFound("Division not found.");
        }

        // Guard: Blocked with 409 Conflict if any Section, User, or UserDivisionAssignment still references it
        var hasSections = _readDbContext.Sections.Any(s => s.DivisionId == request.Id);
        var hasUsers = _readDbContext.Users.Any(u => u.DivisionId == request.Id);
        var hasAssignments = _readDbContext.UserDivisionAssignments.Any(uda => uda.DivisionId == request.Id);

        if (hasSections || hasUsers || hasAssignments)
        {
            return Result.Conflict("Cannot delete division because it is referenced by existing sections, users, or administrator assignments.");
        }

        divisionRepo.Remove(division);

        var now = _dateTimeProvider.UtcNow;
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "division_deleted",
            EntityType = "Division",
            EntityId = division.Id.ToString(),
            Details = $"{{\"Name\":\"{division.Name}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
