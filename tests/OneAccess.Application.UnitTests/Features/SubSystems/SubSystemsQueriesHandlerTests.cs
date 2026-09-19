using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.SubSystems.Queries.GetRoleSubSystems;
using OneAccess.Application.Features.SubSystems.Queries.GetSubSystems;
using OneAccess.Application.Features.SubSystems.Queries.GetUserSubSystems;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.SubSystems;

public class SubSystemsQueriesHandlerTests
{
    private readonly IReadDbContext _readDbContext;

    public SubSystemsQueriesHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
    }

    [Fact]
    public async Task GetSubSystems_ShouldReturnAllSubSystemsIncludingInactive()
    {
        var list = new List<SubSystem>
        {
            new SubSystem { Id = Guid.NewGuid(), Code = "SYS1", Name = "Active System", IsActive = true, Audience = "sys1" },
            new SubSystem { Id = Guid.NewGuid(), Code = "SYS2", Name = "Inactive System", IsActive = false, Audience = "sys2" }
        }.AsQueryable();

        _readDbContext.SubSystems.Returns(list);

        var handler = new GetSubSystemsQueryHandler(_readDbContext);
        var result = await handler.Handle(new GetSubSystemsQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRoleSubSystems_WhenRoleNotFound_ShouldReturnNotFound()
    {
        var roleId = Guid.NewGuid();
        _readDbContext.Roles.Returns(new List<Role>().AsQueryable());

        var handler = new GetRoleSubSystemsQueryHandler(_readDbContext);
        var result = await handler.Handle(new GetRoleSubSystemsQuery(roleId), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetUserSubSystems_WhenUserNotFound_ShouldReturnNotFound()
    {
        var userId = Guid.NewGuid();
        _readDbContext.Users.Returns(new List<User>().AsQueryable());

        var handler = new GetUserSubSystemsQueryHandler(_readDbContext);
        var result = await handler.Handle(new GetUserSubSystemsQuery(userId), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
