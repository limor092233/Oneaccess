using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Permissions.Queries.GetPermissions;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Permissions;

public class GetPermissionsQueryHandlerTests
{
    private readonly IReadDbContext _readDbContext;

    public GetPermissionsQueryHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
    }

    [Fact]
    public async Task GetPermissions_ShouldReturnAllPermissionsOrderedByModuleAndCode()
    {
        var perms = new List<Permission>
        {
            new() { Id = Guid.NewGuid(), Code = "user.create", Module = "Users", Description = "Create user", IsDelegable = true },
            new() { Id = Guid.NewGuid(), Code = "audit.view", Module = "Audit", Description = "View audit logs", IsDelegable = false },
            new() { Id = Guid.NewGuid(), Code = "permission.view", Module = "Permissions", Description = "View permissions", IsDelegable = true }
        }.AsQueryable();

        _readDbContext.Permissions.Returns(perms);

        var handler = new GetPermissionsQueryHandler(_readDbContext);
        var result = await handler.Handle(new GetPermissionsQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value![0].Code.Should().Be("audit.view");
        result.Value[0].IsDelegable.Should().BeFalse();
        result.Value[1].Code.Should().Be("permission.view");
        result.Value[1].IsDelegable.Should().BeTrue();
        result.Value[2].Code.Should().Be("user.create");
        result.Value[2].IsDelegable.Should().BeTrue();
    }
}
