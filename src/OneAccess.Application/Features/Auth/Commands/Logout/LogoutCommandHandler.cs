using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;

namespace OneAccess.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LogoutCommandHandler(
        ICurrentUserService currentUserService,
        IReadDbContext readDbContext,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        ITokenRevocationService tokenRevocationService,
        IDateTimeProvider dateTimeProvider)
    {
        _currentUserService = currentUserService;
        _readDbContext = readDbContext;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _tokenRevocationService = tokenRevocationService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var userId = _currentUserService.UserId;

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);
            var token = _readDbContext.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
            if (token != null)
            {
                userId ??= token.UserId;
                token.RevokedAt = now;
                _unitOfWork.Repository<OneAccess.Domain.Entities.RefreshToken>().Update(token);
            }
        }

        if (userId.HasValue)
        {
            await _tokenRevocationService.RevokeAsync(userId.Value, now, cancellationToken);

            var audit = new AuditLog
            {
                UserId = userId.Value,
                Action = "logout",
                EntityType = "User",
                EntityId = userId.Value.ToString(),
                Details = "{\"Action\":\"UserLoggedOut\"}",
                CreatedAt = now
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(audit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
