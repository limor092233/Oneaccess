using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.RolePermissions.Commands.RevokePermission;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.RolePermissions;

public class RevokePermissionCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<Role> _roleRepo;
    private readonly IRepository<Permission> _permRepo;
    private readonly IRepository<RolePermission> _rolePermRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly RevokePermissionCommandHandler _handler;

    public RevokePermissionCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _cacheService = Substitute.For<ICacheService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _roleRepo = Substitute.For<IRepository<Role>>();
        _permRepo = Substitute.For<IRepository<Permission>>();
        _rolePermRepo = Substitute.For<IRepository<RolePermission>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<Role>().Returns(_roleRepo);
        _unitOfWork.Repository<Permission>().Returns(_permRepo);
        _unitOfWork.Repository<RolePermission>().Returns(_rolePermRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new RevokePermissionCommandHandler(
            _readDbContext,
            _unitOfWork,
            _currentUserService,
            _cacheService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenTargetIsSystemAdministratorRole_ShouldRejectWithBadRequest()
    {
        var roleId = Guid.NewGuid();
        var permId = Guid.NewGuid();
        var sysRole = new Role { Id = roleId, Name = SystemRoles.SystemAdministrator, IsSystemRole = true };
        var perm = new Permission { Id = permId, Code = "user.create" };

        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(sysRole);
        _permRepo.GetByIdAsync(permId, Arg.Any<CancellationToken>()).Returns(perm);

        var command = new RevokePermissionCommand(roleId, permId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(400); // Guard 4: Reject modifying System Administrator permissions
        result.ErrorCode.Should().Be("SystemAdminPermissionsImmutable");
    }

    [Fact]
    public async Task Handle_WhenValidRole_ShouldRevokeAndInvalidateUserCachesWithoutRevokingTokens()
    {
        var roleId = Guid.NewGuid();
        var permId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var role = new Role { Id = roleId, Name = "Administrator", IsSystemRole = false };
        var perm = new Permission { Id = permId, Code = "user.delete" };
        var rolePerm = new RolePermission { RoleId = roleId, PermissionId = permId };

        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
        _permRepo.GetByIdAsync(permId, Arg.Any<CancellationToken>()).Returns(perm);
        _readDbContext.RolePermissions.Returns(new List<RolePermission> { rolePerm }.AsQueryable());
        _readDbContext.UserRoles.Returns(new List<UserRole> { new UserRole { RoleId = roleId, UserId = userId } }.AsQueryable());

        var command = new RevokePermissionCommand(roleId, permId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _rolePermRepo.Received(1).Remove(rolePerm);
        await _cacheService.Received(1).RemoveAsync($"permissions:{userId}", Arg.Any<CancellationToken>());
        await _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
