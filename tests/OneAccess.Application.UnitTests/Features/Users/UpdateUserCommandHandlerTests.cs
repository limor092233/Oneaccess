using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Users.Commands.UpdateUser;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Users;

public class UpdateUserCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _cacheService = Substitute.For<ICacheService>();
        _tokenRevocationService = Substitute.For<ITokenRevocationService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userRepo = Substitute.For<IRepository<User>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<User>().Returns(_userRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new UpdateUserCommandHandler(
            _readDbContext,
            _unitOfWork,
            _currentUserService,
            _cacheService,
            _tokenRevocationService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenNonSysAdminReassignsToUnmanagedDivision_ShouldReturnForbidden()
    {
        var userId = Guid.NewGuid();
        var currentDivisionId = Guid.NewGuid();
        var newDivisionId = Guid.NewGuid();

        var user = new User { Id = userId, DivisionId = currentDivisionId, Email = "test@example.com" };
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _currentUserService.IsSystemAdministratorAsync(Arg.Any<CancellationToken>()).Returns(false);
        _currentUserService.GetAssignedDivisionIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid> { currentDivisionId });

        var command = new UpdateUserCommand(userId, "test@example.com", "Updated Name", newDivisionId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldUpdateUserAndInvalidateCaches()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", FullName = "Old Name", Status = UserStatus.Active };
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _currentUserService.IsSystemAdministratorAsync(Arg.Any<CancellationToken>()).Returns(true);
        _readDbContext.Users.Returns(new List<User> { user }.AsQueryable());

        var command = new UpdateUserCommand(userId, "newemail@example.com", "New Name", null, null, UserStatus.Active);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.FullName.Should().Be("New Name");
        result.Value.Email.Should().Be("newemail@example.com");

        await _cacheService.Received(1).RemoveAsync($"roles:{userId}", Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync($"permissions:{userId}", Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync($"assigned_divisions:{userId}", Arg.Any<CancellationToken>());
        await _tokenRevocationService.Received(1).RevokeAsync(userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
