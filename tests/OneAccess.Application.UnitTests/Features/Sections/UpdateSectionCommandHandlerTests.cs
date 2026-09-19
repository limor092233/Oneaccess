using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Sections.Commands.UpdateSection;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Sections;

public class UpdateSectionCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<Section> _sectionRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly UpdateSectionCommandHandler _handler;

    public UpdateSectionCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _sectionRepo = Substitute.For<IRepository<Section>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<Section>().Returns(_sectionRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new UpdateSectionCommandHandler(
            _unitOfWork,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenSectionNotFound_ShouldReturnNotFound()
    {
        _sectionRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Section?)null);

        var command = new UpdateSectionCommand(Guid.NewGuid(), "Backend", "Backend dev");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldUpdateSection()
    {
        var sectionId = Guid.NewGuid();
        var section = new Section { Id = sectionId, Name = "Old Name", Description = "Old Desc" };
        _sectionRepo.GetByIdAsync(sectionId, Arg.Any<CancellationToken>()).Returns(section);

        var command = new UpdateSectionCommand(sectionId, "New Name", "New Desc");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        section.Name.Should().Be("New Name");
        section.Description.Should().Be("New Desc");

        _sectionRepo.Received(1).Update(section);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
