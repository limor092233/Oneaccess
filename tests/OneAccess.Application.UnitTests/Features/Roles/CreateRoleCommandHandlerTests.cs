using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Roles.Commands.CreateRole;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Roles;

public class CreateRoleCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<Role> _roleRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly CreateRoleCommandHandler _handler;

    public CreateRoleCommandHandlerTests()
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

        _handler = new CreateRoleCommandHandler(
            _readDbContext,
            _unitOfWork,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenRoleNameExists_ShouldReturnConflict()
    {
        var existingRole = new Role { Name = "Manager" };
        _readDbContext.Roles.Returns(new List<Role> { existingRole }.AsQueryable());

        var command = new CreateRoleCommand("Manager", "Manager role");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreateRole()
    {
        _readDbContext.Roles.Returns(new List<Role>().AsQueryable());

        var command = new CreateRoleCommand("Manager", "Manager role");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Name.Should().Be("Manager");
        result.Value.IsSystemRole.Should().BeFalse();

        await _roleRepo.Received(1).AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
