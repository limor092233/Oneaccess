using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.SubSystems.Commands.AssignUserSubSystemAccess;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.SubSystems;

public class AssignUserSubSystemAccessCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<SubSystem> _subSystemRepo;
    private readonly IRepository<UserSubSystemAccess> _accessRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly AssignUserSubSystemAccessCommandHandler _handler;

    public AssignUserSubSystemAccessCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _cacheService = Substitute.For<ICacheService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userRepo = Substitute.For<IRepository<User>>();
        _subSystemRepo = Substitute.For<IRepository<SubSystem>>();
        _accessRepo = Substitute.For<IRepository<UserSubSystemAccess>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<User>().Returns(_userRepo);
        _unitOfWork.Repository<SubSystem>().Returns(_subSystemRepo);
        _unitOfWork.Repository<UserSubSystemAccess>().Returns(_accessRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new AssignUserSubSystemAccessCommandHandler(
            _readDbContext,
            _unitOfWork,
            _currentUserService,
            _cacheService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFound()
    {
        var userId = Guid.NewGuid();
        var subSystemId = Guid.NewGuid();
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var command = new AssignUserSubSystemAccessCommand(userId, subSystemId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldAssignAndInvalidateUserCache()
    {
        var userId = Guid.NewGuid();
        var subSystemId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "testuser" };
        var subSystem = new SubSystem { Id = subSystemId, Code = "SYS1", Audience = "sys1.api" };

        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _subSystemRepo.GetByIdAsync(subSystemId, Arg.Any<CancellationToken>()).Returns(subSystem);
        _readDbContext.UserSubSystemAccesses.Returns(new List<UserSubSystemAccess>().AsQueryable());

        var command = new AssignUserSubSystemAccessCommand(userId, subSystemId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        await _accessRepo.Received(1).AddAsync(Arg.Is<UserSubSystemAccess>(u => u.UserId == userId && u.SubSystemId == subSystemId), Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync($"subsystems:{userId}", Arg.Any<CancellationToken>());
        await _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
