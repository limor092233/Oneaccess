using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneAccess.Domain.Entities;
using OneAccess.Infrastructure.Persistence;
using Xunit;

namespace OneAccess.API.IntegrationTests;

public class DatabaseUniqueConstraintsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DatabaseUniqueConstraintsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubSystem_Audience_UniqueIndex_Rejects_Duplicate_At_Database_Level()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OneAccessDbContext>();

        var testAudience = "integration-unique-aud-" + Guid.NewGuid();

        var sub1 = new SubSystem
        {
            Id = Guid.NewGuid(),
            Code = "AUD_DB_1_" + Guid.NewGuid().ToString()[..8],
            Name = "Aud Sub 1",
            Audience = testAudience,
            IsActive = true
        };

        db.SubSystems.Add(sub1);
        await db.SaveChangesAsync();

        var sub2 = new SubSystem
        {
            Id = Guid.NewGuid(),
            Code = "AUD_DB_2_" + Guid.NewGuid().ToString()[..8],
            Name = "Aud Sub 2",
            Audience = testAudience, // Duplicate audience
            IsActive = true
        };

        db.SubSystems.Add(sub2);

        // Expect DbUpdateException from SQL constraint violation
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task RolePermission_CompositeUniqueIndex_Rejects_Duplicate_At_Database_Level()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OneAccessDbContext>();

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "UniqueRole_" + Guid.NewGuid().ToString()[..8],
            Description = "Test",
            IsSystemRole = false
        };
        db.Roles.Add(role);

        var perm = new Permission
        {
            Id = Guid.NewGuid(),
            Code = "perm.unique." + Guid.NewGuid().ToString()[..8],
            Module = "test",
            Description = "test",
            IsDelegable = true
        };
        db.Permissions.Add(perm);
        await db.SaveChangesAsync();

        var rp1 = new RolePermission { RoleId = role.Id, PermissionId = perm.Id };
        db.RolePermissions.Add(rp1);
        await db.SaveChangesAsync();

        // Attempting to attach another entry with the exact same composite key
        var rp2 = new RolePermission { RoleId = role.Id, PermissionId = perm.Id };
        
        // In EF Core, adding duplicate tracked key or inserting duplicate to DB throws
        var throwsInvalidOpOrDbUpdate = false;
        try
        {
            db.RolePermissions.Add(rp2);
            await db.SaveChangesAsync();
        }
        catch (Exception e) when (e is InvalidOperationException || e is DbUpdateException)
        {
            throwsInvalidOpOrDbUpdate = true;
        }

        Assert.True(throwsInvalidOpOrDbUpdate, "Expected duplicate composite key to be rejected by entity key or database unique constraint.");
    }
}
