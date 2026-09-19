using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.SubSystems.Queries.GetMySubSystems;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.SubSystems;

public class GetMySubSystemsQueryHandlerTests
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubSystemAccessService _subSystemAccessService;
    private readonly GetMySubSystemsQueryHandler _handler;

    public GetMySubSystemsQueryHandlerTests()
    {
        _currentUserService = Substitute.For<ICurrentUserService>();
        _subSystemAccessService = Substitute.For<ISubSystemAccessService>();

        _handler = new GetMySubSystemsQueryHandler(_currentUserService, _subSystemAccessService);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var query = new GetMySubSystemsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_ShouldCallAccessServiceAndMapDtos()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        var accessible = new List<SubSystem>
        {
            new SubSystem { Id = Guid.NewGuid(), Code = "SYS1", Name = "System 1", BaseUrl = "https://sys1", Audience = "sys1.api", IsActive = true }
        };
        _subSystemAccessService.GetAccessibleSubSystemsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(accessible);

        var query = new GetMySubSystemsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Code.Should().Be("SYS1");
    }
}
