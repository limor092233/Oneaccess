using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<CreateRoleResponse>>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateRoleCommandHandler(
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

    public async Task<Result<CreateRoleResponse>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        var exists = _readDbContext.Roles
            .Any(r => r.Name.ToLower() == normalizedName.ToLower());

        if (exists)
        {
            return Result<CreateRoleResponse>.Conflict("A role with this name already exists.");
        }

        var roleRepo = _unitOfWork.Repository<Role>();
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var now = _dateTimeProvider.UtcNow;

        var role = new Role
        {
            Name = normalizedName,
            Description = request.Description?.Trim() ?? string.Empty,
            IsSystemRole = false,
            CreatedAt = now
        };

        await roleRepo.AddAsync(role, cancellationToken);

        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "role_created",
            EntityType = "Role",
            EntityId = role.Id.ToString(),
            Details = $"{{\"Name\":\"{role.Name}\",\"Description\":\"{role.Description}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CreateRoleResponse>.Success(new CreateRoleResponse(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.CreatedAt));
    }
}
