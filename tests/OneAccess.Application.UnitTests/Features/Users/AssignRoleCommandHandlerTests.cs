using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Users.Commands.AssignRole;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Users;

public class AssignRoleCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Role> _roleRepo;
    private readonly IRepository<UserRole> _userRoleRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly AssignRoleCommandHandler _handler;

    public AssignRoleCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _cacheService = Substitute.For<ICacheService>();
        _tokenRevocationService = Substitute.For<ITokenRevocationService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userRepo = Substitute.For<IRepository<User>>();
        _roleRepo = Substitute.For<IRepository<Role>>();
        _userRoleRepo = Substitute.For<IRepository<UserRole>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<User>().Returns(_userRepo);
        _unitOfWork.Repository<Role>().Returns(_roleRepo);
        _unitOfWork.Repository<UserRole>().Returns(_userRoleRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new AssignRoleCommandHandler(
            _readDbContext,
            _unitOfWork,
            _currentUserService,
            _cacheService,
            _tokenRevocationService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenNonSysAdminAssignsSystemAdministratorRole_ShouldReturnForbidden()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "testuser" };
        var role = new Role { Id = roleId, Name = SystemRoles.SystemAdministrator, IsSystemRole = true };

        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
        _currentUserService.IsSystemAdministratorAsync(Arg.Any<CancellationToken>()).Returns(false); // Non-SysAdmin caller

        var command = new AssignRoleCommand(userId, roleId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Handle_WhenNonSysAdminAssignsStandardUserRole_ShouldSucceed()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "testuser" };
        var role = new Role { Id = roleId, Name = SystemRoles.User, IsSystemRole = true }; // Built-in role with IsSystemRole=true

        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
        _currentUserService.IsSystemAdministratorAsync(Arg.Any<CancellationToken>()).Returns(false); // Non-SysAdmin caller
        _readDbContext.UserRoles.Returns(new List<UserRole>().AsQueryable());

        var command = new AssignRoleCommand(userId, roleId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        await _userRoleRepo.Received(1).AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSysAdminAssignsSystemAdminRole_ShouldSetUserFlagAndInvalidateCache()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "secondadmin", IsSystemAdministrator = false };
        var role = new Role { Id = roleId, Name = SystemRoles.SystemAdministrator, IsSystemRole = true };

        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
        _currentUserService.IsSystemAdministratorAsync(Arg.Any<CancellationToken>()).Returns(true);
        _readDbContext.UserRoles.Returns(new List<UserRole>().AsQueryable());

        var command = new AssignRoleCommand(userId, roleId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        user.IsSystemAdministrator.Should().BeTrue();

        await _cacheService.Received(1).RemoveAsync($"roles:{userId}", Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync($"permissions:{userId}", Arg.Any<CancellationToken>());
        await _tokenRevocationService.Received(1).RevokeAsync(userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
