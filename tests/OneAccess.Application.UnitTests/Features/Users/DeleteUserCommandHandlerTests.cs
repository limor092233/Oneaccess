using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Users.Commands.DeleteUser;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Users;

public class DeleteUserCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
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

        _handler = new DeleteUserCommandHandler(
            _unitOfWork,
            _currentUserService,
            _cacheService,
            _tokenRevocationService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldPerformSoftDeleteAndInvalidateCache()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Status = UserStatus.Active, Username = "testuser" };
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var command = new DeleteUserCommand(userId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Inactive); // Guard 1: Soft delete

        _userRepo.Received(1).Update(user);
        _userRepo.DidNotReceive().Remove(Arg.Any<User>()); // Must NEVER be hard delete

        await _cacheService.Received(1).RemoveAsync($"roles:{userId}", Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync($"permissions:{userId}", Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync($"assigned_divisions:{userId}", Arg.Any<CancellationToken>());
        await _tokenRevocationService.Received(1).RevokeAsync(userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
