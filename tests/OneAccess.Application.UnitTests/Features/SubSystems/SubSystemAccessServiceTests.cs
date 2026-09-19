using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using OneAccess.Infrastructure.Persistence;
using OneAccess.Infrastructure.Services;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.SubSystems;

public class SubSystemAccessServiceTests
{
    private readonly OneAccessDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly SubSystemAccessService _service;

    public SubSystemAccessServiceTests()
    {
        var options = new DbContextOptionsBuilder<OneAccessDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new OneAccessDbContext(options);
        _cacheService = Substitute.For<ICacheService>();
        _service = new SubSystemAccessService(_dbContext, _cacheService);
    }

    [Fact]
    public async Task GetAccessibleSubSystemsAsync_WhenUserInactive_ShouldReturnEmpty()
    {
        var userId = Guid.NewGuid();
        var inactiveUser = new User
        {
            Id = userId,
            Username = "inactive",
            Email = "inactive@test.com",
            PasswordHash = "hash",
            FullName = "Inactive User",
            Status = UserStatus.Inactive
        };
        await _dbContext.Users.AddAsync(inactiveUser);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAccessibleSubSystemsAsync(userId, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccessibleSubSystemsAsync_WhenSystemAdministrator_ShouldReturnAllActiveSubSystems()
    {
        var userId = Guid.NewGuid();
        var sysAdmin = new User
        {
            Id = userId,
            Username = "sysadmin",
            Email = "sysadmin@test.com",
            PasswordHash = "hash",
            FullName = "Sys Admin",
            Status = UserStatus.Active,
            IsSystemAdministrator = true
        };
        await _dbContext.Users.AddAsync(sysAdmin);

        var activeSubSystem = new SubSystem { Id = Guid.NewGuid(), Code = "SYS1", Name = "Sys 1", Audience = "sys1", IsActive = true };
        var inactiveSubSystem = new SubSystem { Id = Guid.NewGuid(), Code = "SYS2", Name = "Sys 2", Audience = "sys2", IsActive = false };
        await _dbContext.SubSystems.AddRangeAsync(activeSubSystem, inactiveSubSystem);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAccessibleSubSystemsAsync(userId, CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Code.Should().Be("SYS1");
    }

    [Fact]
    public async Task HasAccessAsync_WhenSubSystemIsActiveAndAccessible_ShouldReturnTrue()
    {
        var userId = Guid.NewGuid();
        var sysAdmin = new User
        {
            Id = userId,
            Username = "sysadmin2",
            Email = "sysadmin2@test.com",
            PasswordHash = "hash",
            FullName = "Sys Admin 2",
            Status = UserStatus.Active,
            IsSystemAdministrator = true
        };
        await _dbContext.Users.AddAsync(sysAdmin);

        var activeId = Guid.NewGuid();
        var activeSubSystem = new SubSystem { Id = activeId, Code = "SYS1", Name = "Sys 1", Audience = "sys1", IsActive = true };
        await _dbContext.SubSystems.AddAsync(activeSubSystem);
        await _dbContext.SaveChangesAsync();

        var hasAccess = await _service.HasAccessAsync(userId, activeId, CancellationToken.None);

        hasAccess.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccessAsync_WhenSubSystemIsInactive_ShouldReturnFalse()
    {
        var userId = Guid.NewGuid();
        var sysAdmin = new User
        {
            Id = userId,
            Username = "sysadmin3",
            Email = "sysadmin3@test.com",
            PasswordHash = "hash",
            FullName = "Sys Admin 3",
            Status = UserStatus.Active,
            IsSystemAdministrator = true
        };
        await _dbContext.Users.AddAsync(sysAdmin);

        var inactiveId = Guid.NewGuid();
        var inactiveSubSystem = new SubSystem { Id = inactiveId, Code = "SYS2", Name = "Sys 2", Audience = "sys2", IsActive = false };
        await _dbContext.SubSystems.AddAsync(inactiveSubSystem);
        await _dbContext.SaveChangesAsync();

        var hasAccess = await _service.HasAccessAsync(userId, inactiveId, CancellationToken.None);

        hasAccess.Should().BeFalse();
    }
}
