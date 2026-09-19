using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Users.Commands.CreateUser;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Users;

public class CreateUserCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userRepo = Substitute.For<IRepository<User>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<User>().Returns(_userRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("hashed_pwd");

        _handler = new CreateUserCommandHandler(
            _readDbContext,
            _unitOfWork,
            _passwordHasher,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenNonSysAdminCreatesUserWithoutDivision_ShouldReturnForbidden()
    {
        _currentUserService.IsSystemAdministratorAsync(Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateUserCommand("testuser", "test@example.com", "Password123!", "Test User", null, null);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Handle_WhenUsernameAlreadyExists_ShouldReturnConflict()
    {
        _currentUserService.IsSystemAdministratorAsync(Arg.Any<CancellationToken>()).Returns(true);
        var existingUser = new User { Username = "existinguser", Email = "other@example.com" };
        _readDbContext.Users.Returns(new List<User> { existingUser }.AsQueryable());

        var command = new CreateUserCommand("existinguser", "new@example.com", "Password123!", "New User");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_WhenNonSysAdminAssignsSystemRole_ShouldReturnForbidden()
    {
        _currentUserService.IsSystemAdministratorAsync(Arg.Any<CancellationToken>()).Returns(false);
        var divisionId = Guid.NewGuid();
        var sysRoleId = Guid.NewGuid();
        var sysRole = new Role { Id = sysRoleId, Name = SystemRoles.SystemAdministrator, IsSystemRole = true };

        _readDbContext.Users.Returns(new List<User>().AsQueryable());
        _readDbContext.Roles.Returns(new List<Role> { sysRole }.AsQueryable());

        var command = new CreateUserCommand("testuser", "test@example.com", "Password123!", "Test User", divisionId, null, new[] { sysRoleId });
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Handle_WhenValidSysAdmin_ShouldCreateUserAndAuditLog()
    {
        _currentUserService.IsSystemAdministratorAsync(Arg.Any<CancellationToken>()).Returns(true);
        _readDbContext.Users.Returns(new List<User>().AsQueryable());

        var command = new CreateUserCommand("newuser", "new@example.com", "Password123!", "New User");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Username.Should().Be("newuser");
        result.Value.Status.Should().Be(UserStatus.Active);

        await _userRepo.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _auditRepo.Received(1).AddAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
