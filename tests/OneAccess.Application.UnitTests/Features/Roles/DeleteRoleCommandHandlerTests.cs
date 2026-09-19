using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Roles.Commands.DeleteRole;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Roles;

public class DeleteRoleCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<Role> _roleRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly DeleteRoleCommandHandler _handler;

    public DeleteRoleCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _roleRepo = Substitute.For<IRepository<Role>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<Role>().Returns(_roleRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new DeleteRoleCommandHandler(
            _readDbContext,
            _unitOfWork,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenDeletingSystemRole_ShouldRejectWithConflict()
    {
        var roleId = Guid.NewGuid();
        var sysRole = new Role { Id = roleId, Name = SystemRoles.SystemAdministrator, IsSystemRole = true };
        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(sysRole);

        var command = new DeleteRoleCommand(roleId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409); // Guard 3: Reject deleting IsSystemRole
    }

    [Fact]
    public async Task Handle_WhenRoleAssignedToUsers_ShouldRejectWithConflict()
    {
        var roleId = Guid.NewGuid();
        var customRole = new Role { Id = roleId, Name = "Auditor", IsSystemRole = false };
        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(customRole);
        _readDbContext.UserRoles.Returns(new List<UserRole> { new UserRole { RoleId = roleId, UserId = Guid.NewGuid() } }.AsQueryable());

        var command = new DeleteRoleCommand(roleId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409); // Guard 3: Reject deleting role in use
    }

    [Fact]
    public async Task Handle_WhenUnassignedCustomRole_ShouldDeleteSuccessfully()
    {
        var roleId = Guid.NewGuid();
        var customRole = new Role { Id = roleId, Name = "Auditor", IsSystemRole = false };
        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(customRole);
        _readDbContext.UserRoles.Returns(new List<UserRole>().AsQueryable());

        var command = new DeleteRoleCommand(roleId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _roleRepo.Received(1).Remove(customRole);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
