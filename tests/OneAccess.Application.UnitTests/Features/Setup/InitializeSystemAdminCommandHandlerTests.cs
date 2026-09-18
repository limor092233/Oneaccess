using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Setup.Commands.InitializeSystemAdmin;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Setup;

public class InitializeSystemAdminCommandHandlerTests
{
    private readonly ISetupCodeService _setupCodeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Role> _roleRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly InitializeSystemAdminCommandHandler _handler;

    public InitializeSystemAdminCommandHandlerTests()
    {
        _setupCodeService = Substitute.For<ISetupCodeService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userRepo = Substitute.For<IRepository<User>>();
        _roleRepo = Substitute.For<IRepository<Role>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<User>().Returns(_userRepo);
        _unitOfWork.Repository<Role>().Returns(_roleRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new InitializeSystemAdminCommandHandler(
            _setupCodeService,
            _unitOfWork,
            _passwordHasher,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenSetupNotRequired_ShouldReturnForbidden()
    {
        _setupCodeService.IsSetupRequiredAsync(Arg.Any<CancellationToken>()).Returns(false);

        var command = new InitializeSystemAdminCommand("12345678", "admin", "admin@oneaccess.local", "Password123!", "Admin");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Handle_WhenCodeInvalid_ShouldReturnFailure()
    {
        _setupCodeService.IsSetupRequiredAsync(Arg.Any<CancellationToken>()).Returns(true);
        _setupCodeService.ValidateAndConsumeCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = new InitializeSystemAdminCommand("wrongcode", "admin", "admin@oneaccess.local", "Password123!", "Admin");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreateUserAndReturnSuccess()
    {
        _setupCodeService.IsSetupRequiredAsync(Arg.Any<CancellationToken>()).Returns(true);
        _setupCodeService.ValidateAndConsumeCodeAsync("validcode", Arg.Any<CancellationToken>()).Returns(true);
        _passwordHasher.HashPassword("Password123!").Returns("hashed_pwd");

        var command = new InitializeSystemAdminCommand("validcode", "admin", "admin@oneaccess.local", "Password123!", "Admin User");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Username.Should().Be("admin");

        await _userRepo.Received(1).AddAsync(Arg.Is<User>(u => u.IsSystemAdministrator && u.Username == "admin"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
