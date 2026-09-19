using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.SubSystems.Commands.RegisterSubSystem;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.SubSystems;

public class RegisterSubSystemCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<SubSystem> _subSystemRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly RegisterSubSystemCommandHandler _handler;

    public RegisterSubSystemCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _subSystemRepo = Substitute.For<IRepository<SubSystem>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<SubSystem>().Returns(_subSystemRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new RegisterSubSystemCommandHandler(
            _readDbContext,
            _unitOfWork,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ShouldReturnConflict()
    {
        var existing = new List<SubSystem>
        {
            new SubSystem { Id = Guid.NewGuid(), Code = "SYS1", Audience = "sys1.api" }
        }.AsQueryable();
        _readDbContext.SubSystems.Returns(existing);

        var command = new RegisterSubSystemCommand("SYS1", "System 1", "https://sys1.local", "sys1_new.api");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorCode.Should().Be("DuplicateSubSystemCode");
    }

    [Fact]
    public async Task Handle_WhenAudienceAlreadyExists_ShouldReturnConflict()
    {
        var existing = new List<SubSystem>
        {
            new SubSystem { Id = Guid.NewGuid(), Code = "SYS1", Audience = "sys1.api" }
        }.AsQueryable();
        _readDbContext.SubSystems.Returns(existing);

        var command = new RegisterSubSystemCommand("SYS2", "System 2", "https://sys2.local", "sys1.api");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorCode.Should().Be("DuplicateSubSystemAudience");
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldRegisterAndWriteAuditLog()
    {
        _readDbContext.SubSystems.Returns(new List<SubSystem>().AsQueryable());

        var command = new RegisterSubSystemCommand("SYS1", "System 1", "https://sys1.local", "sys1.api");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Code.Should().Be("SYS1");
        result.Value.Name.Should().Be("System 1");
        result.Value.Audience.Should().Be("sys1.api");

        await _subSystemRepo.Received(1).AddAsync(Arg.Is<SubSystem>(s => s.Code == "SYS1"), Arg.Any<CancellationToken>());
        await _auditRepo.Received(1).AddAsync(Arg.Is<AuditLog>(a => a.Action == "subsystem_registered"), Arg.Any<CancellationToken>());
        await _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
