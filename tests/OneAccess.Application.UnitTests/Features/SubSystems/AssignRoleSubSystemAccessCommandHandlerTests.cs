using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.SubSystems.Commands.AssignRoleSubSystemAccess;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.SubSystems;

public class AssignRoleSubSystemAccessCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<Role> _roleRepo;
    private readonly IRepository<SubSystem> _subSystemRepo;
    private readonly IRepository<RoleSubSystemAccess> _accessRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly AssignRoleSubSystemAccessCommandHandler _handler;

    public AssignRoleSubSystemAccessCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _cacheService = Substitute.For<ICacheService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _roleRepo = Substitute.For<IRepository<Role>>();
        _subSystemRepo = Substitute.For<IRepository<SubSystem>>();
        _accessRepo = Substitute.For<IRepository<RoleSubSystemAccess>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<Role>().Returns(_roleRepo);
        _unitOfWork.Repository<SubSystem>().Returns(_subSystemRepo);
        _unitOfWork.Repository<RoleSubSystemAccess>().Returns(_accessRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new AssignRoleSubSystemAccessCommandHandler(
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
        var subSystemId = Guid.NewGuid();
        var sysAdminRole = new Role { Id = roleId, Name = SystemRoles.SystemAdministrator, IsSystemRole = true };
        var subSystem = new SubSystem { Id = subSystemId, Code = "SYS1", Audience = "sys1.api" };

        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(sysAdminRole);
        _subSystemRepo.GetByIdAsync(subSystemId, Arg.Any<CancellationToken>()).Returns(subSystem);

        var command = new AssignRoleSubSystemAccessCommand(roleId, subSystemId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(400); // Guard 1: Reject targeting System Administrator role
        result.ErrorCode.Should().Be("SystemAdminSubSystemRestrictionNotAllowed");
    }

    [Fact]
    public async Task Handle_WhenValidRole_ShouldAssignAndInvalidateRoleUsersCache()
    {
        var roleId = Guid.NewGuid();
        var subSystemId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userRole = new Role { Id = roleId, Name = SystemRoles.User, IsSystemRole = true };
        var subSystem = new SubSystem { Id = subSystemId, Code = "SYS1", Audience = "sys1.api" };

        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(userRole);
        _subSystemRepo.GetByIdAsync(subSystemId, Arg.Any<CancellationToken>()).Returns(subSystem);
        _readDbContext.RoleSubSystemAccesses.Returns(new List<RoleSubSystemAccess>().AsQueryable());
        _readDbContext.UserRoles.Returns(new List<UserRole> { new UserRole { RoleId = roleId, UserId = userId } }.AsQueryable());

        var command = new AssignRoleSubSystemAccessCommand(roleId, subSystemId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        await _accessRepo.Received(1).AddAsync(Arg.Is<RoleSubSystemAccess>(r => r.RoleId == roleId && r.SubSystemId == subSystemId), Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync($"subsystems:{userId}", Arg.Any<CancellationToken>());
        await _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
