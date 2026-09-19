using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Divisions.Commands.DeleteDivision;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Divisions;

public class DeleteDivisionCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<Division> _divisionRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly DeleteDivisionCommandHandler _handler;

    public DeleteDivisionCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _divisionRepo = Substitute.For<IRepository<Division>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<Division>().Returns(_divisionRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new DeleteDivisionCommandHandler(
            _readDbContext,
            _unitOfWork,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenDivisionNotFound_ShouldReturnNotFound()
    {
        _divisionRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Division?)null);

        var command = new DeleteDivisionCommand(Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenDivisionReferencedBySection_ShouldReturnConflict()
    {
        var divisionId = Guid.NewGuid();
        var division = new Division { Id = divisionId, Name = "Finance" };
        _divisionRepo.GetByIdAsync(divisionId, Arg.Any<CancellationToken>()).Returns(division);

        _readDbContext.Sections.Returns(new List<Section>
        {
            new Section { DivisionId = divisionId, Name = "Payroll" }
        }.AsQueryable());
        _readDbContext.Users.Returns(new List<User>().AsQueryable());
        _readDbContext.UserDivisionAssignments.Returns(new List<UserDivisionAssignment>().AsQueryable());

        var command = new DeleteDivisionCommand(divisionId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_WhenDivisionReferencedByUser_ShouldReturnConflict()
    {
        var divisionId = Guid.NewGuid();
        var division = new Division { Id = divisionId, Name = "Finance" };
        _divisionRepo.GetByIdAsync(divisionId, Arg.Any<CancellationToken>()).Returns(division);

        _readDbContext.Sections.Returns(new List<Section>().AsQueryable());
        _readDbContext.Users.Returns(new List<User>
        {
            new User { DivisionId = divisionId, Username = "alice" }
        }.AsQueryable());
        _readDbContext.UserDivisionAssignments.Returns(new List<UserDivisionAssignment>().AsQueryable());

        var command = new DeleteDivisionCommand(divisionId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_WhenDivisionReferencedByAssignment_ShouldReturnConflict()
    {
        var divisionId = Guid.NewGuid();
        var division = new Division { Id = divisionId, Name = "Finance" };
        _divisionRepo.GetByIdAsync(divisionId, Arg.Any<CancellationToken>()).Returns(division);

        _readDbContext.Sections.Returns(new List<Section>().AsQueryable());
        _readDbContext.Users.Returns(new List<User>().AsQueryable());
        _readDbContext.UserDivisionAssignments.Returns(new List<UserDivisionAssignment>
        {
            new UserDivisionAssignment { DivisionId = divisionId, UserId = Guid.NewGuid() }
        }.AsQueryable());

        var command = new DeleteDivisionCommand(divisionId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_WhenUnreferenced_ShouldDeleteDivision()
    {
        var divisionId = Guid.NewGuid();
        var division = new Division { Id = divisionId, Name = "Finance" };
        _divisionRepo.GetByIdAsync(divisionId, Arg.Any<CancellationToken>()).Returns(division);

        _readDbContext.Sections.Returns(new List<Section>().AsQueryable());
        _readDbContext.Users.Returns(new List<User>().AsQueryable());
        _readDbContext.UserDivisionAssignments.Returns(new List<UserDivisionAssignment>().AsQueryable());

        var command = new DeleteDivisionCommand(divisionId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _divisionRepo.Received(1).Remove(division);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
