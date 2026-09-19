using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Divisions.Commands.UpdateDivision;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Divisions;

public class UpdateDivisionCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<Division> _divisionRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly UpdateDivisionCommandHandler _handler;

    public UpdateDivisionCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _divisionRepo = Substitute.For<IRepository<Division>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<Division>().Returns(_divisionRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new UpdateDivisionCommandHandler(
            _unitOfWork,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenDivisionNotFound_ShouldReturnNotFound()
    {
        _divisionRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Division?)null);

        var command = new UpdateDivisionCommand(Guid.NewGuid(), "HR", "Human Resources");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldUpdateDivision()
    {
        var division = new Division { Id = Guid.NewGuid(), Name = "Old Name", Description = "Old Desc" };
        _divisionRepo.GetByIdAsync(division.Id, Arg.Any<CancellationToken>()).Returns(division);

        var command = new UpdateDivisionCommand(division.Id, "New Name", "New Desc");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        division.Name.Should().Be("New Name");
        division.Description.Should().Be("New Desc");

        _divisionRepo.Received(1).Update(division);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
