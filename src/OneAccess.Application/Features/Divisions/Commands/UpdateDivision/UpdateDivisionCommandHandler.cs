using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.Divisions.Commands.UpdateDivision;

public class UpdateDivisionCommandHandler : IRequestHandler<UpdateDivisionCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateDivisionCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateDivisionCommand request, CancellationToken cancellationToken)
    {
        var divisionRepo = _unitOfWork.Repository<Division>();
        var division = await divisionRepo.GetByIdAsync(request.Id, cancellationToken);
        if (division == null)
        {
            return Result.NotFound("Division not found.");
        }

        division.Name = request.Name.Trim();
        division.Description = request.Description?.Trim() ?? string.Empty;

        divisionRepo.Update(division);

        var now = _dateTimeProvider.UtcNow;
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "division_updated",
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
