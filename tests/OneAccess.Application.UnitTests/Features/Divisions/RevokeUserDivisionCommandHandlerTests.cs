using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Divisions.Commands.RevokeUserDivision;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Divisions;

public class RevokeUserDivisionCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<Division> _divisionRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<UserDivisionAssignment> _assignmentRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly RevokeUserDivisionCommandHandler _handler;

    public RevokeUserDivisionCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _cacheService = Substitute.For<ICacheService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _divisionRepo = Substitute.For<IRepository<Division>>();
        _userRepo = Substitute.For<IRepository<User>>();
        _assignmentRepo = Substitute.For<IRepository<UserDivisionAssignment>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<Division>().Returns(_divisionRepo);
        _unitOfWork.Repository<User>().Returns(_userRepo);
        _unitOfWork.Repository<UserDivisionAssignment>().Returns(_assignmentRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new RevokeUserDivisionCommandHandler(
            _readDbContext,
            _unitOfWork,
            _currentUserService,
            _cacheService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldRevokeAndInvalidateCache()
    {
        var divisionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var division = new Division { Id = divisionId, Name = "Div A" };
        var user = new User { Id = userId, Username = "admin1" };
        var assignment = new UserDivisionAssignment { DivisionId = divisionId, UserId = userId };

        _divisionRepo.GetByIdAsync(divisionId, Arg.Any<CancellationToken>()).Returns(division);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _readDbContext.UserDivisionAssignments.Returns(new List<UserDivisionAssignment> { assignment }.AsQueryable());

        var command = new RevokeUserDivisionCommand(divisionId, userId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _assignmentRepo.Received(1).Remove(assignment);
        await _cacheService.Received(1).RemoveAsync($"assigned_divisions:{userId}", Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
