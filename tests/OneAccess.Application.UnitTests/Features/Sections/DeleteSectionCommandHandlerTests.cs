using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Sections.Commands.DeleteSection;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Sections;

public class DeleteSectionCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<Section> _sectionRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly DeleteSectionCommandHandler _handler;

    public DeleteSectionCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _sectionRepo = Substitute.For<IRepository<Section>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<Section>().Returns(_sectionRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new DeleteSectionCommandHandler(
            _readDbContext,
            _unitOfWork,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenSectionNotFound_ShouldReturnNotFound()
    {
        _sectionRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Section?)null);

        var command = new DeleteSectionCommand(Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenReferencedByUser_ShouldReturnConflict()
    {
        var sectionId = Guid.NewGuid();
        var section = new Section { Id = sectionId, Name = "Core" };
        _sectionRepo.GetByIdAsync(sectionId, Arg.Any<CancellationToken>()).Returns(section);

        _readDbContext.Users.Returns(new List<User>
        {
            new User { SectionId = sectionId, Username = "bob" }
        }.AsQueryable());

        var command = new DeleteSectionCommand(sectionId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_WhenUnreferenced_ShouldDeleteSection()
    {
        var sectionId = Guid.NewGuid();
        var section = new Section { Id = sectionId, Name = "Core" };
        _sectionRepo.GetByIdAsync(sectionId, Arg.Any<CancellationToken>()).Returns(section);

        _readDbContext.Users.Returns(new List<User>().AsQueryable());

        var command = new DeleteSectionCommand(sectionId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _sectionRepo.Received(1).Remove(section);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
