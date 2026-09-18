using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISubSystemAccessService _subSystemAccessService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoginCommandHandler(
        IReadDbContext readDbContext,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ISubSystemAccessService subSystemAccessService,
        IDateTimeProvider dateTimeProvider)
    {
        _readDbContext = readDbContext;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _subSystemAccessService = subSystemAccessService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = _readDbContext.Users.FirstOrDefault(u => u.Username == request.Username.Trim());
        if (user == null || user.Status != UserStatus.Active || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            // Generic message for security rule 7
            return Result<LoginResponse>.Unauthorized("Invalid username or password.");
        }

        string? audience = null;
        if (!string.IsNullOrWhiteSpace(request.SubSystemCode))
        {
            var subSystem = _readDbContext.SubSystems.FirstOrDefault(s => s.Code == request.SubSystemCode && s.IsActive);
            if (subSystem == null)
            {
                return Result<LoginResponse>.NotFound($"Sub-system with code '{request.SubSystemCode}' not found or inactive.");
            }

            var hasAccess = await _subSystemAccessService.HasAccessAsync(user.Id, subSystem.Id, cancellationToken);
            if (!hasAccess)
            {
                return Result<LoginResponse>.Forbidden($"Access to sub-system '{subSystem.Name}' is not allowed.");
            }

            audience = subSystem.Audience;
        }

        // Get user role names
        var roleNames = (from ur in _readDbContext.UserRoles
                         join r in _readDbContext.Roles on ur.RoleId equals r.Id
                         where ur.UserId == user.Id
                         select r.Name).ToList();

        var accessToken = _jwtTokenService.GenerateAccessToken(user, roleNames, audience);
        var (refreshTokenString, refreshTokenHash) = _jwtTokenService.GenerateRefreshToken();

        var now = _dateTimeProvider.UtcNow;
        var refreshToken = new OneAccess.Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = now.AddDays(7)
        };

        await _unitOfWork.Repository<OneAccess.Domain.Entities.RefreshToken>().AddAsync(refreshToken, cancellationToken);

        // Audit Log
        var audit = new AuditLog
        {
            UserId = user.Id,
            Action = "login_success",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Details = $"{{\"Username\":\"{user.Username}\",\"Audience\":\"{audience ?? "OneAccess.Client"}\"}}",
            CreatedAt = now
        };
        await _unitOfWork.Repository<AuditLog>().AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userInfo = new LoginUserInfo(user.Id, user.Username, user.Email, user.FullName, user.IsSystemAdministrator);
        return Result<LoginResponse>.Success(new LoginResponse(accessToken, refreshTokenString, 15, userInfo));
    }
}
