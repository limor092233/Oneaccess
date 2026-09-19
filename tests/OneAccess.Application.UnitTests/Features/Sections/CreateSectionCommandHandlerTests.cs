using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Sections.Commands.CreateSection;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Sections;

public class CreateSectionCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<Division> _divisionRepo;
    private readonly IRepository<Section> _sectionRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly CreateSectionCommandHandler _handler;

    public CreateSectionCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _divisionRepo = Substitute.For<IRepository<Division>>();
        _sectionRepo = Substitute.For<IRepository<Section>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<Division>().Returns(_divisionRepo);
        _unitOfWork.Repository<Section>().Returns(_sectionRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new CreateSectionCommandHandler(
            _unitOfWork,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenDivisionNotFound_ShouldReturnNotFound()
    {
        _divisionRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Division?)null);

        var command = new CreateSectionCommand(Guid.NewGuid(), "Backend", "Backend dev");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreateSection()
    {
        var divisionId = Guid.NewGuid();
        _divisionRepo.GetByIdAsync(divisionId, Arg.Any<CancellationToken>()).Returns(new Division { Id = divisionId });

        var command = new CreateSectionCommand(divisionId, "Backend", "Backend dev");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Name.Should().Be("Backend");
        result.Value.DivisionId.Should().Be(divisionId);

        await _sectionRepo.Received(1).AddAsync(Arg.Any<Section>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
