using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.SubSystems.Commands.UpdateSubSystem;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.SubSystems;

public class UpdateSubSystemCommandHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<SubSystem> _subSystemRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly UpdateSubSystemCommandHandler _handler;

    public UpdateSubSystemCommandHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _cacheService = Substitute.For<ICacheService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _subSystemRepo = Substitute.For<IRepository<SubSystem>>();
        _auditRepo = Substitute.For<IRepository<AuditLog>>();

        _unitOfWork.Repository<SubSystem>().Returns(_subSystemRepo);
        _unitOfWork.Repository<AuditLog>().Returns(_auditRepo);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _handler = new UpdateSubSystemCommandHandler(
            _readDbContext,
            _unitOfWork,
            _currentUserService,
            _cacheService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenSubSystemNotFound_ShouldReturnNotFound()
    {
        var id = Guid.NewGuid();
        _subSystemRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((SubSystem?)null);

        var command = new UpdateSubSystemCommand(id, "Updated Name", "https://url", "aud", true);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenDuplicateAudienceOnAnotherSubSystem_ShouldReturnConflict()
    {
        var id = Guid.NewGuid();
        var subSystem = new SubSystem { Id = id, Code = "SYS1", Audience = "sys1.api" };
        _subSystemRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(subSystem);

        var otherSubSystems = new List<SubSystem>
        {
            new SubSystem { Id = Guid.NewGuid(), Code = "SYS2", Audience = "sys2.api" }
        }.AsQueryable();
        _readDbContext.SubSystems.Returns(otherSubSystems);

        var command = new UpdateSubSystemCommand(id, "Updated Name", "https://url", "sys2.api", true);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorCode.Should().Be("DuplicateSubSystemAudience");
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldUpdateAndInvalidateAffectedCaches()
    {
        var id = Guid.NewGuid();
        var subSystem = new SubSystem { Id = id, Code = "SYS1", Name = "Old Name", BaseUrl = "https://old", Audience = "sys1.api", IsActive = true };
        _subSystemRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(subSystem);
        _readDbContext.SubSystems.Returns(new List<SubSystem> { subSystem }.AsQueryable());

        var userId = Guid.NewGuid();
        _readDbContext.UserSubSystemAccesses.Returns(new List<UserSubSystemAccess> { new UserSubSystemAccess { UserId = userId, SubSystemId = id } }.AsQueryable());
        _readDbContext.RoleSubSystemAccesses.Returns(new List<RoleSubSystemAccess>().AsQueryable());
        _readDbContext.UserRoles.Returns(new List<UserRole>().AsQueryable());

        var command = new UpdateSubSystemCommand(id, "New Name", "https://new", "sys1.api", false);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Name");
        result.Value.IsActive.Should().BeFalse();

        _subSystemRepo.Received(1).Update(Arg.Is<SubSystem>(s => s.Name == "New Name" && !s.IsActive));
        await _cacheService.Received(1).RemoveAsync($"subsystems:{userId}", Arg.Any<CancellationToken>());
        await _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
