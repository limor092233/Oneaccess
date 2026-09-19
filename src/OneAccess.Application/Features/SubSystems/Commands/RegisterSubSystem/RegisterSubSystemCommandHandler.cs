using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.SubSystems.Commands.RegisterSubSystem;

/// <summary>
/// Handler for RegisterSubSystemCommand.
/// </summary>
public class RegisterSubSystemCommandHandler : IRequestHandler<RegisterSubSystemCommand, Result<RegisterSubSystemResponse>>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterSubSystemCommandHandler(
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

    public async Task<Result<RegisterSubSystemResponse>> Handle(RegisterSubSystemCommand request, CancellationToken cancellationToken)
    {
        var codeExists = _readDbContext.SubSystems.Any(s => s.Code == request.Code);
        if (codeExists)
        {
            return Result<RegisterSubSystemResponse>.Failure($"A sub-system with code '{request.Code}' already exists.", "DuplicateSubSystemCode", 409);
        }

        var audienceExists = _readDbContext.SubSystems.Any(s => s.Audience == request.Audience);
        if (audienceExists)
        {
            return Result<RegisterSubSystemResponse>.Failure($"A sub-system with audience '{request.Audience}' already exists.", "DuplicateSubSystemAudience", 409);
        }

        var now = _dateTimeProvider.UtcNow;
        var subSystem = new SubSystem
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            BaseUrl = request.BaseUrl,
            Audience = request.Audience,
            IsActive = request.IsActive
        };

        var repo = _unitOfWork.Repository<SubSystem>();
        await repo.AddAsync(subSystem, cancellationToken);

        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "subsystem_registered",
            EntityType = "SubSystem",
            EntityId = subSystem.Id.ToString(),
            Details = $"{{\"Code\":\"{subSystem.Code}\",\"Name\":\"{subSystem.Name}\",\"Audience\":\"{subSystem.Audience}\",\"IsActive\":{subSystem.IsActive.ToString().ToLowerInvariant()}}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new RegisterSubSystemResponse(
            subSystem.Id,
            subSystem.Code,
            subSystem.Name,
            subSystem.BaseUrl,
            subSystem.Audience,
            subSystem.IsActive
        );

        return Result<RegisterSubSystemResponse>.Success(response);
    }
}
