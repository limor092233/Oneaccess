using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.Sections.Commands.DeleteSection;

public class DeleteSectionCommandHandler : IRequestHandler<DeleteSectionCommand, Result>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteSectionCommandHandler(
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

    public async Task<Result> Handle(DeleteSectionCommand request, CancellationToken cancellationToken)
    {
        var sectionRepo = _unitOfWork.Repository<Section>();
        var section = await sectionRepo.GetByIdAsync(request.Id, cancellationToken);
        if (section == null)
        {
            return Result.NotFound("Section not found.");
        }

        // Guard: Blocked with 409 Conflict if any User still references it via SectionId
        var hasUsers = _readDbContext.Users.Any(u => u.SectionId == request.Id);
        if (hasUsers)
        {
            return Result.Conflict("Cannot delete section because it is referenced by existing users.");
        }

        sectionRepo.Remove(section);

        var now = _dateTimeProvider.UtcNow;
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "section_deleted",
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
