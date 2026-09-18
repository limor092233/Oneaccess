using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Auth.Commands.RefreshToken;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly ISubSystemAccessService _subSystemAccessService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<RefreshToken> _refreshTokenRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _tokenRevocationService = Substitute.For<ITokenRevocationService>();
        _subSystemAccessService = Substitute.For<ISubSystemAccessService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _refreshTokenRepo = Substitute.For<IRepository<RefreshToken>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<RefreshToken>().Returns(_refreshTokenRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new RefreshTokenCommandHandler(
            _readDbContext,
            _unitOfWork,
            _jwtTokenService,
            _tokenRevocationService,
            _subSystemAccessService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenTokenNotFoundOrExpired_ShouldReturnUnauthorized()
    {
        _jwtTokenService.HashToken("invalid_token").Returns("hash_invalid");
        _readDbContext.RefreshTokens.Returns(new List<RefreshToken>().AsQueryable());

        var command = new RefreshTokenCommand("invalid_token");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_WhenTokenReused_ShouldRevokeAllAndReturnUnauthorized()
    {
        var userId = Guid.NewGuid();
        var compromisedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash_compromised",
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            RevokedAt = DateTime.UtcNow.AddHours(-1),
            ReplacedByTokenId = Guid.NewGuid()
        };

        _jwtTokenService.HashToken("compromised_token").Returns("hash_compromised");
        _readDbContext.RefreshTokens.Returns(new List<RefreshToken> { compromisedToken }.AsQueryable());

        var command = new RefreshTokenCommand("compromised_token");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.Error.Should().Contain("reuse detected");

        await _tokenRevocationService.Received(1).RevokeAsync(userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldRotateTokenAndReturnSuccess()
    {
        var userId = Guid.NewGuid();
        var validToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash_valid",
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            RevokedAt = null,
            ReplacedByTokenId = null
        };

        var user = new User
        {
            Id = userId,
            Username = "johndoe",
            Status = UserStatus.Active
        };

        _jwtTokenService.HashToken("raw_token").Returns("hash_valid");
        _readDbContext.RefreshTokens.Returns(new List<RefreshToken> { validToken }.AsQueryable());
        _readDbContext.Users.Returns(new List<User> { user }.AsQueryable());
        _readDbContext.UserRoles.Returns(new List<UserRole>().AsQueryable());
        _readDbContext.Roles.Returns(new List<Role>().AsQueryable());

        _jwtTokenService.GenerateRefreshToken().Returns(("new_raw_token", "new_hash"));
        _jwtTokenService.GenerateAccessToken(user, Arg.Any<IEnumerable<string>>(), null).Returns("new_access_token");

        var command = new RefreshTokenCommand("raw_token");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("new_access_token");
        result.Value!.RefreshToken.Should().Be("new_raw_token");

        validToken.RevokedAt.Should().NotBeNull();
        validToken.ReplacedByTokenId.Should().NotBeNull();
        await _refreshTokenRepo.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
