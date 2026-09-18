using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Auth.Commands.Login;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISubSystemAccessService _subSystemAccessService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<RefreshToken> _refreshTokenRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _subSystemAccessService = Substitute.For<ISubSystemAccessService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _refreshTokenRepo = Substitute.For<IRepository<RefreshToken>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<RefreshToken>().Returns(_refreshTokenRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new LoginCommandHandler(
            _readDbContext,
            _unitOfWork,
            _passwordHasher,
            _jwtTokenService,
            _subSystemAccessService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnUnauthorized()
    {
        _readDbContext.Users.Returns(new List<User>().AsQueryable());

        var command = new LoginCommand("unknown", "password");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_WhenPasswordIncorrect_ShouldReturnUnauthorized()
    {
        var user = new User
        {
            Username = "johndoe",
            PasswordHash = "correct_hash",
            Status = UserStatus.Active
        };
        _readDbContext.Users.Returns(new List<User> { user }.AsQueryable());
        _passwordHasher.VerifyPassword("wrong_password", "correct_hash").Returns(false);

        var command = new LoginCommand("johndoe", "wrong_password");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldIssueTokensAndReturnSuccess()
    {
        var user = new User
        {
            Username = "johndoe",
            PasswordHash = "correct_hash",
            Status = UserStatus.Active,
            Email = "john@example.com",
            FullName = "John Doe"
        };
        _readDbContext.Users.Returns(new List<User> { user }.AsQueryable());
        _readDbContext.UserRoles.Returns(new List<UserRole>().AsQueryable());
        _readDbContext.Roles.Returns(new List<Role>().AsQueryable());

        _passwordHasher.VerifyPassword("password123", "correct_hash").Returns(true);
        _jwtTokenService.GenerateAccessToken(user, Arg.Any<IEnumerable<string>>(), null).Returns("jwt_access_token");
        _jwtTokenService.GenerateRefreshToken().Returns(("raw_refresh_token", "hash_refresh_token"));

        var command = new LoginCommand("johndoe", "password123");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("jwt_access_token");
        result.Value!.RefreshToken.Should().Be("raw_refresh_token");
        result.Value!.User.Username.Should().Be("johndoe");

        await _refreshTokenRepo.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
