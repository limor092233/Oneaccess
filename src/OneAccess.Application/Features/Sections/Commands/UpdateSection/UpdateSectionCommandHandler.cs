using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.Sections.Commands.UpdateSection;

public class UpdateSectionCommandHandler : IRequestHandler<UpdateSectionCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateSectionCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateSectionCommand request, CancellationToken cancellationToken)
    {
        var sectionRepo = _unitOfWork.Repository<Section>();
        var section = await sectionRepo.GetByIdAsync(request.Id, cancellationToken);
        if (section == null)
        {
            return Result.NotFound("Section not found.");
        }

        section.Name = request.Name.Trim();
        section.Description = request.Description?.Trim() ?? string.Empty;

        sectionRepo.Update(section);

        var now = _dateTimeProvider.UtcNow;
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "section_updated",
            EntityType = "Section",
            EntityId = section.Id.ToString(),
            Details = $"{{\"DivisionId\":\"{section.DivisionId}\",\"Name\":\"{section.Name}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
