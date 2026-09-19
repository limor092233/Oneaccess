using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.Sections.Commands.CreateSection;

public class CreateSectionCommandHandler : IRequestHandler<CreateSectionCommand, Result<CreateSectionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateSectionCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CreateSectionResponse>> Handle(CreateSectionCommand request, CancellationToken cancellationToken)
    {
        var divisionRepo = _unitOfWork.Repository<Division>();
        var division = await divisionRepo.GetByIdAsync(request.DivisionId, cancellationToken);
        if (division == null)
        {
            return Result<CreateSectionResponse>.NotFound("Division not found.");
        }

        var now = _dateTimeProvider.UtcNow;
        var section = new Section
        {
            DivisionId = division.Id,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            CreatedAt = now
        };

        var sectionRepo = _unitOfWork.Repository<Section>();
        await sectionRepo.AddAsync(section, cancellationToken);

        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "section_created",
            EntityType = "Section",
            EntityId = section.Id.ToString(),
            Details = $"{{\"DivisionId\":\"{division.Id}\",\"Name\":\"{section.Name}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CreateSectionResponse>.Success(
            new CreateSectionResponse(section.Id, section.DivisionId, section.Name, section.Description, section.CreatedAt));
    }
}
