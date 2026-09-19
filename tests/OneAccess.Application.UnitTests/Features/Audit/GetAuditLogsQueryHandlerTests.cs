using FluentAssertions;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Audit.Queries.GetAuditLogs;
using OneAccess.Domain.Entities;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Audit;

public class GetAuditLogsQueryHandlerTests
{
    private readonly IReadDbContext _readDbContext;

    public GetAuditLogsQueryHandlerTests()
    {
        _readDbContext = Substitute.For<IReadDbContext>();
    }

    [Fact]
    public async Task GetAuditLogs_Unfiltered_ShouldReturnPagedResultsOrderedByCreatedAtDescending()
    {
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var baseDate = new DateTime(2026, 9, 19, 10, 0, 0, DateTimeKind.Utc);

        var logs = new List<AuditLog>
        {
            new() { Id = Guid.NewGuid(), UserId = user1Id, Action = "user_create", EntityType = "User", EntityId = "1", Details = "{}", CreatedAt = baseDate.AddMinutes(10) },
            new() { Id = Guid.NewGuid(), UserId = user2Id, Action = "role_create", EntityType = "Role", EntityId = "2", Details = "{}", CreatedAt = baseDate.AddMinutes(20) },
            new() { Id = Guid.NewGuid(), UserId = user1Id, Action = "permission_assigned", EntityType = "RolePermission", EntityId = "3", Details = "{}", CreatedAt = baseDate.AddMinutes(30) }
        }.AsQueryable();

        _readDbContext.AuditLogs.Returns(logs);

        var handler = new GetAuditLogsQueryHandler(_readDbContext);
        var result = await handler.Handle(new GetAuditLogsQuery(PageNumber: 1, PageSize: 2), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(3);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(2);
        result.Value.TotalPages.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Action.Should().Be("permission_assigned"); // Latest first
        result.Value.Items[1].Action.Should().Be("role_create");
    }

    [Fact]
    public async Task GetAuditLogs_FilteredByEntityTypeAndDateRange_ShouldReturnMatchingEntries()
    {
        var user1Id = Guid.NewGuid();
        var baseDate = new DateTime(2026, 9, 19, 10, 0, 0, DateTimeKind.Utc);

        var logs = new List<AuditLog>
        {
            new() { Id = Guid.NewGuid(), UserId = user1Id, Action = "role_create", EntityType = "Role", EntityId = "1", Details = "{}", CreatedAt = baseDate.AddMinutes(10) },
            new() { Id = Guid.NewGuid(), UserId = user1Id, Action = "role_rename", EntityType = "Role", EntityId = "1", Details = "{}", CreatedAt = baseDate.AddMinutes(20) },
            new() { Id = Guid.NewGuid(), UserId = user1Id, Action = "user_create", EntityType = "User", EntityId = "2", Details = "{}", CreatedAt = baseDate.AddMinutes(25) },
            new() { Id = Guid.NewGuid(), UserId = user1Id, Action = "role_delete", EntityType = "Role", EntityId = "1", Details = "{}", CreatedAt = baseDate.AddMinutes(50) }
        }.AsQueryable();

        _readDbContext.AuditLogs.Returns(logs);

        var handler = new GetAuditLogsQueryHandler(_readDbContext);
        var query = new GetAuditLogsQuery(
            PageNumber: 1,
            PageSize: 10,
            EntityType: "Role",
            FromUtc: baseDate.AddMinutes(5),
            ToUtc: baseDate.AddMinutes(30)
        );

        var result = await handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Select(x => x.Action).Should().BeEquivalentTo(new[] { "role_create", "role_rename" });
    }

    [Fact]
    public void Validator_WhenToUtcIsBeforeFromUtc_ShouldHaveValidationError()
    {
        var validator = new GetAuditLogsQueryValidator();
        var from = DateTime.UtcNow;
        var to = from.AddHours(-1);

        var query = new GetAuditLogsQuery(FromUtc: from, ToUtc: to);
        var validationResult = validator.Validate(query);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "ToUtc");
    }
}
