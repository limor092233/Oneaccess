using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Roles.Commands.UpdateRole;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Roles;

public class UpdateRoleCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<Role> _roleRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly UpdateRoleCommandHandler _handler;

    public UpdateRoleCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _cacheService = Substitute.For<ICacheService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _roleRepo = Substitute.For<IRepository<Role>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<Role>().Returns(_roleRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new UpdateRoleCommandHandler(
            _readDbContext,
            _unitOfWork,
            _currentUserService,
            _cacheService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenRenamingSystemRole_ShouldRejectWithBadRequest()
    {
        var roleId = Guid.NewGuid();
        var systemRole = new Role { Id = roleId, Name = SystemRoles.SystemAdministrator, IsSystemRole = true };
        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(systemRole);

        var command = new UpdateRoleCommand(roleId, "Super Administrator", "New Description");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(400); // Guard 2: UpdateRole rejects renaming any Role where IsSystemRole == true
        result.ErrorCode.Should().Be("SystemRoleRenamingNotAllowed");
    }

    [Fact]
    public async Task Handle_WhenValidCustomRole_ShouldUpdateAndInvalidateUserCaches()
    {
        var roleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var customRole = new Role { Id = roleId, Name = "Old Role", Description = "Old Desc", IsSystemRole = false };
        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(customRole);
        _readDbContext.Roles.Returns(new List<Role> { customRole }.AsQueryable());
        _readDbContext.UserRoles.Returns(new List<UserRole> { new UserRole { RoleId = roleId, UserId = userId } }.AsQueryable());

        var command = new UpdateRoleCommand(roleId, "New Role", "New Desc");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        customRole.Name.Should().Be("New Role");

        await _cacheService.Received(1).RemoveAsync($"roles:{userId}", Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync($"permissions:{userId}", Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
