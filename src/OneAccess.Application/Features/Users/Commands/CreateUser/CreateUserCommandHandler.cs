using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<CreateUserResponse>>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateUserCommandHandler(
        IReadDbContext readDbContext,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _readDbContext = readDbContext;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CreateUserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var isSysAdmin = await _currentUserService.IsSystemAdministratorAsync(cancellationToken);

        // Non-SysAdmin administrators cannot create a user with no Division
        if (!isSysAdmin && !request.DivisionId.HasValue)
        {
            return Result<CreateUserResponse>.Forbidden("Administrators must assign users to a division they manage.");
        }

        // Check username uniqueness
        var usernameExists = _readDbContext.Users
            .Any(u => u.Username.ToLower() == request.Username.Trim().ToLower());
        if (usernameExists)
        {
            return Result<CreateUserResponse>.Conflict("A user with this username already exists.");
        }

        // Check email uniqueness
        var emailExists = _readDbContext.Users
            .Any(u => u.Email.ToLower() == request.Email.Trim().ToLower());
        if (emailExists)
        {
            return Result<CreateUserResponse>.Conflict("A user with this email address already exists.");
        }

        var userRepo = _unitOfWork.Repository<User>();
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var now = _dateTimeProvider.UtcNow;

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Status = UserStatus.Active,
            IsSystemAdministrator = false,
            DivisionId = request.DivisionId,
            SectionId = request.SectionId,
            CreatedAt = now
        };

        if (request.RoleIds != null && request.RoleIds.Count > 0)
        {
            var roles = _readDbContext.Roles
                .Where(r => request.RoleIds.Contains(r.Id))
                .ToList();

            // Guard 5: Only System Administrator can assign system roles
            if (roles.Any(r => r.IsSystemRole) && !isSysAdmin)
            {
                return Result<CreateUserResponse>.Forbidden("Only System Administrators can assign system roles.");
            }

            foreach (var role in roles)
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });

                if (string.Equals(role.Name, SystemRoles.SystemAdministrator, StringComparison.OrdinalIgnoreCase))
                {
                    user.IsSystemAdministrator = true;
                }
            }
        }

        await userRepo.AddAsync(user, cancellationToken);

        var audit = new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "user_created",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Details = $"{{\"Username\":\"{user.Username}\",\"Email\":\"{user.Email}\",\"DivisionId\":\"{user.DivisionId}\",\"SectionId\":\"{user.SectionId}\"}}",
            CreatedAt = now
        };
        await auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new CreateUserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.DivisionId,
            user.SectionId,
            user.Status,
            user.CreatedAt);

        return Result<CreateUserResponse>.Success(response);
    }
}
