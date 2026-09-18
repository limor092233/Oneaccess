using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using RefreshTokenEntity = OneAccess.Domain.Entities.RefreshToken;

namespace OneAccess.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly ISubSystemAccessService _subSystemAccessService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenCommandHandler(
        IReadDbContext readDbContext,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        ITokenRevocationService tokenRevocationService,
        ISubSystemAccessService subSystemAccessService,
        IDateTimeProvider dateTimeProvider)
    {
        _readDbContext = readDbContext;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _tokenRevocationService = tokenRevocationService;
        _subSystemAccessService = subSystemAccessService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);
        var tokenRecord = _readDbContext.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        var now = _dateTimeProvider.UtcNow;

        if (tokenRecord == null || tokenRecord.ExpiresAt < now)
        {
            return Result<RefreshTokenResponse>.Unauthorized("Invalid or expired refresh token.");
        }

        // Reuse detection check per Section 10
        if (tokenRecord.RevokedAt != null || tokenRecord.ReplacedByTokenId != null)
        {
            // Theft detected: revoke all user tokens
            await _tokenRevocationService.RevokeAsync(tokenRecord.UserId, now, cancellationToken);

            var allUserTokens = _readDbContext.RefreshTokens
                .Where(t => t.UserId == tokenRecord.UserId && t.RevokedAt == null)
                .ToList();

            var tokenRepo = _unitOfWork.Repository<RefreshTokenEntity>();
            foreach (var t in allUserTokens)
            {
                t.RevokedAt = now;
                tokenRepo.Update(t);
            }

            var audit = new AuditLog
            {
                UserId = tokenRecord.UserId,
                Action = "refresh_token_reuse_detected",
                EntityType = "RefreshToken",
                EntityId = tokenRecord.Id.ToString(),
                Details = $"{{\"RevokedTokenId\":\"{tokenRecord.Id}\",\"Action\":\"AllSessionsRevoked\"}}",
                CreatedAt = now
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(audit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<RefreshTokenResponse>.Unauthorized("Refresh token reuse detected. All sessions revoked.");
        }

        // Valid token rotation
        var user = _readDbContext.Users.FirstOrDefault(u => u.Id == tokenRecord.UserId);
        if (user == null || user.Status != UserStatus.Active)
        {
            return Result<RefreshTokenResponse>.Unauthorized("User account is inactive or not found.");
        }

        string? audience = null;
        if (!string.IsNullOrWhiteSpace(request.SubSystemCode))
        {
            var subSystem = _readDbContext.SubSystems.FirstOrDefault(s => s.Code == request.SubSystemCode && s.IsActive);
            if (subSystem == null)
            {
                return Result<RefreshTokenResponse>.NotFound($"Sub-system with code '{request.SubSystemCode}' not found.");
            }

            var hasAccess = await _subSystemAccessService.HasAccessAsync(user.Id, subSystem.Id, cancellationToken);
            if (!hasAccess)
            {
                return Result<RefreshTokenResponse>.Forbidden($"Access to sub-system '{subSystem.Name}' is not allowed.");
            }

            audience = subSystem.Audience;
        }

        var (newRefreshTokenString, newRefreshTokenHash) = _jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = now.AddDays(7)
        };

        await _unitOfWork.Repository<RefreshTokenEntity>().AddAsync(newRefreshToken, cancellationToken);

        // Mark old token revoked and replaced
        tokenRecord.RevokedAt = now;
        tokenRecord.ReplacedByTokenId = newRefreshToken.Id;
        _unitOfWork.Repository<RefreshTokenEntity>().Update(tokenRecord);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var roleNames = (from ur in _readDbContext.UserRoles
                         join r in _readDbContext.Roles on ur.RoleId equals r.Id
                         where ur.UserId == user.Id
                         select r.Name).ToList();

        var accessToken = _jwtTokenService.GenerateAccessToken(user, roleNames, audience);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(accessToken, newRefreshTokenString, 15));
    }
}
