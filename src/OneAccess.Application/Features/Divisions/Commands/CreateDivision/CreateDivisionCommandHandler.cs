using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.Divisions.Commands.CreateDivision;

public class CreateDivisionCommandHandler : IRequestHandler<CreateDivisionCommand, Result<CreateDivisionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateDivisionCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CreateDivisionResponse>> Handle(CreateDivisionCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var division = new Division
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            CreatedAt = now
        };

        var divisionRepo = _unitOfWork.Repository<Division>();
        await divisionRepo.AddAsync(division, cancellationToken);

        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "division_created",
            EntityType = "Division",
            EntityId = division.Id.ToString(),
            Details = $"{{\"Name\":\"{division.Name}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CreateDivisionResponse>.Success(
            new CreateDivisionResponse(division.Id, division.Name, division.Description, division.CreatedAt));
    }
}
