using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Divisions.Queries.GetDivisions;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Divisions;

public class GetDivisionsQueryHandlerTests
{
    private readonly IReadDbContext _readDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly GetDivisionsQueryHandler _handler;

    public GetDivisionsQueryHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _handler = new GetDivisionsQueryHandler(_readDbContext, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenSysAdmin_ShouldReturnAllDivisions()
    {
        _currentUserService.IsSystemAdministratorAsync(Arg.Any<CancellationToken>()).Returns(true);

        var div1 = new Division { Id = Guid.NewGuid(), Name = "Div 1" };
        var div2 = new Division { Id = Guid.NewGuid(), Name = "Div 2" };
        _readDbContext.Divisions.Returns(new List<Division> { div1, div2 }.AsQueryable());

        var result = await _handler.Handle(new GetDivisionsQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Count.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenNonSysAdmin_ShouldReturnOnlyAssignedDivisions()
    {
        _currentUserService.IsSystemAdministratorAsync(Arg.Any<CancellationToken>()).Returns(false);

        var div1 = new Division { Id = Guid.NewGuid(), Name = "Div 1" };
        var div2 = new Division { Id = Guid.NewGuid(), Name = "Div 2" };
        _readDbContext.Divisions.Returns(new List<Division> { div1, div2 }.AsQueryable());
        _currentUserService.GetAssignedDivisionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { div1.Id });

        var result = await _handler.Handle(new GetDivisionsQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Count.Should().Be(1);
        result.Value[0].Id.Should().Be(div1.Id);
    }
}
