using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.Setup.Commands.InitializeSystemAdmin;

public class InitializeSystemAdminCommandHandler : IRequestHandler<InitializeSystemAdminCommand, Result<InitializeSystemAdminResponse>>
{
    private readonly ISetupCodeService _setupCodeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InitializeSystemAdminCommandHandler(
        ISetupCodeService setupCodeService,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider)
    {
        _setupCodeService = setupCodeService;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<InitializeSystemAdminResponse>> Handle(InitializeSystemAdminCommand request, CancellationToken cancellationToken)
    {
        var isSetupRequired = await _setupCodeService.IsSetupRequiredAsync(cancellationToken);
        if (!isSetupRequired)
        {
            return Result<InitializeSystemAdminResponse>.Forbidden("Setup has already been completed.");
        }

        var isCodeValid = await _setupCodeService.ValidateAndConsumeCodeAsync(request.Code, cancellationToken);
        if (!isCodeValid)
        {
            return Result<InitializeSystemAdminResponse>.Failure("Invalid or expired setup code.", "InvalidSetupCode", 400);
        }

        var userRepo = _unitOfWork.Repository<User>();
        var roleRepo = _unitOfWork.Repository<Role>();
        var auditRepo = _unitOfWork.Repository<AuditLog>();

        // Find or create System Administrator role
        var sysAdminRole = await roleRepo.FindAsync(r => r.Name == SystemRoles.SystemAdministrator, cancellationToken);
        if (sysAdminRole == null)
        {
            sysAdminRole = new Role
            {
                Name = SystemRoles.SystemAdministrator,
                Description = "System Administrator with full access.",
                IsSystemRole = true,
                CreatedAt = _dateTimeProvider.UtcNow
            };
            await roleRepo.AddAsync(sysAdminRole, cancellationToken);
        }

        // Create System Admin user
        var now = _dateTimeProvider.UtcNow;
        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Status = UserStatus.Active,
            IsSystemAdministrator = true,
            CreatedAt = now
        };

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = sysAdminRole.Id,
            User = user,
            Role = sysAdminRole
        });

        await userRepo.AddAsync(user, cancellationToken);

        // Audit Log
        var audit = new AuditLog
        {
            UserId = user.Id,
            Action = "setup_initialized",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Details = $"{{\"Username\":\"{user.Username}\",\"IsSystemAdministrator\":true}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<InitializeSystemAdminResponse>.Success(new InitializeSystemAdminResponse(user.Id, user.Username));
    }
}
