using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Auth.Queries.GetCurrentUser;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Auth;

public class GetCurrentUserQueryHandlerTests
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IReadDbContext _readDbContext;
    private readonly ISubSystemAccessService _subSystemAccessService;
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _currentUserService = Substitute.For<ICurrentUserService>();
        _readDbContext = Substitute.For<IReadDbContext>();
        _subSystemAccessService = Substitute.For<ISubSystemAccessService>();

        _handler = new GetCurrentUserQueryHandler(
            _currentUserService,
            _readDbContext,
            _subSystemAccessService);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        _currentUserService.IsAuthenticated.Returns(false);

        var query = new GetCurrentUserQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_ShouldReturnUserDataAndPermissions()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "sysadmin",
            Email = "admin@oneaccess.local",
            FullName = "System Admin",
            IsSystemAdministrator = true,
            Status = UserStatus.Active
        };

        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(userId);
        _currentUserService.Roles.Returns(new List<string> { "System Administrator" });
        _currentUserService.GetRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<string> { "System Administrator" });
        _currentUserService.IsSystemAdministratorAsync(Arg.Any<CancellationToken>()).Returns(true);
        _currentUserService.GetPermissionsAsync(Arg.Any<CancellationToken>()).Returns(new List<string> { "user.create", "user.view" });
        _subSystemAccessService.GetAccessibleSubSystemsAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<SubSystem>());
        _readDbContext.Users.Returns(new List<User> { user }.AsQueryable());

        var query = new GetCurrentUserQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Username.Should().Be("sysadmin");
        result.Value!.Permissions.Should().Contain("user.create");
        result.Value!.IsSystemAdministrator.Should().BeTrue();
    }
}
