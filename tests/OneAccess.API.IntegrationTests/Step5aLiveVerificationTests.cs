using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Auth.Commands.Login;
using OneAccess.Application.Features.Auth.Queries.GetCurrentUser;
using OneAccess.Application.Features.Setup.Commands.InitializeSystemAdmin;
using OneAccess.Application.Features.Users.Commands.CreateUser;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using OneAccess.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace OneAccess.API.IntegrationTests;

public class OneAccessApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OneAccessDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<OneAccessDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }
}

public class Step5aLiveVerificationTests : IClassFixture<OneAccessApiFactory>
{
    private readonly OneAccessApiFactory _factory;
    private readonly ITestOutputHelper _output;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public Step5aLiveVerificationTests(OneAccessApiFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    private async Task<(HttpClient Client, Guid SysAdminUserId, Guid SysAdminRoleId)> SetupAndLoginRootSysAdminAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });

        using var scope = _factory.Services.CreateScope();
        var setupCodeService = scope.ServiceProvider.GetRequiredService<ISetupCodeService>();
        var db = scope.ServiceProvider.GetRequiredService<OneAccessDbContext>();

        var code = await setupCodeService.GenerateAndStoreCodeAsync();

        await client.PostAsJsonAsync("/api/setup/initialize", new InitializeSystemAdminCommand(
            code,
            "rootadmin",
            "root@oneaccess.local",
            "RootAdmin123!",
            "Root Administrator"
        ));

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(
            "rootadmin",
            "RootAdmin123!"
        ));
        loginResponse.EnsureSuccessStatusCode();

        var sysAdminRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.SystemAdministrator);
        var sysAdminUser = await db.Users.FirstAsync(u => u.Username == "rootadmin");

        return (client, sysAdminUser.Id, sysAdminRole.Id);
    }

    [Fact]
    public async Task Run_And_Log_All_Step5a_Verification_Scenarios()
    {
        var (sysAdminClient, sysAdminUserId, sysAdminRoleId) = await SetupAndLoginRootSysAdminAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OneAccessDbContext>();

        // Seed Divisions and Sections
        var div1 = new Division { Id = Guid.NewGuid(), Name = "Division 1", Description = "Div 1" };
        var div2 = new Division { Id = Guid.NewGuid(), Name = "Division 2", Description = "Div 2" };
        var sec2 = new Section { Id = Guid.NewGuid(), DivisionId = div2.Id, Name = "Section 2", Description = "Sec under Div 2" };

        await db.Divisions.AddRangeAsync(div1, div2);
        await db.Sections.AddAsync(sec2);
        await db.SaveChangesAsync();

        _output.WriteLine("================================================================================");
        _output.WriteLine("STEP 5A LIVE HTTP VERIFICATION RUN");
        _output.WriteLine("================================================================================\n");

        // -------------------------------------------------------------
        // Scenario 1: Attempt to rename the "System Administrator" role -> rejected (400)
        // -------------------------------------------------------------
        _output.WriteLine("### Scenario 1: Attempt to rename 'System Administrator' role");
        var renamePayload = new { name = "Super Administrator", description = "Renamed Description" };
        var renameJson = JsonSerializer.Serialize(renamePayload, _jsonOptions);
        _output.WriteLine($"Request: PUT /api/roles/{sysAdminRoleId}\n{renameJson}");
        var renameResponse = await sysAdminClient.PutAsJsonAsync($"/api/roles/{sysAdminRoleId}", renamePayload);
        var renameContent = await renameResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {(int)renameResponse.StatusCode} {renameResponse.StatusCode}\n{renameContent}\n");
        renameResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // -------------------------------------------------------------
        // Scenario 2: Attempt to delete the "System Administrator" role -> rejected (409)
        // -------------------------------------------------------------
        _output.WriteLine("### Scenario 2: Attempt to delete 'System Administrator' role");
        _output.WriteLine($"Request: DELETE /api/roles/{sysAdminRoleId}");
        var deleteRoleResponse = await sysAdminClient.DeleteAsync($"/api/roles/{sysAdminRoleId}");
        var deleteRoleContent = await deleteRoleResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {(int)deleteRoleResponse.StatusCode} {deleteRoleResponse.StatusCode}\n{deleteRoleContent}\n");
        deleteRoleResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // -------------------------------------------------------------
        // Scenario 3: Attempt to revoke a permission from "System Administrator" -> rejected (400)
        // -------------------------------------------------------------
        var permToRevoke = await db.Permissions.FirstAsync(p => p.Code == "user.create");
        _output.WriteLine("### Scenario 3: Attempt to revoke a permission from 'System Administrator' role");
        _output.WriteLine($"Request: DELETE /api/roles/{sysAdminRoleId}/permissions/{permToRevoke.Id}");
        var revokePermResponse = await sysAdminClient.DeleteAsync($"/api/roles/{sysAdminRoleId}/permissions/{permToRevoke.Id}");
        var revokePermContent = await revokePermResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {(int)revokePermResponse.StatusCode} {revokePermResponse.StatusCode}\n{revokePermContent}\n");
        revokePermResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // -------------------------------------------------------------
        // Scenario 4: Attempt to assign "System Administrator" role to a user as a caller who is NOT a System Administrator -> rejected (403)
        // -------------------------------------------------------------
        _output.WriteLine("### Scenario 4: Non-SysAdmin assigns 'System Administrator' role");
        var adminRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.Administrator);
        var userUpdatePerm = await db.Permissions.FirstAsync(p => p.Code == "user.update");
        var userViewPerm = await db.Permissions.FirstAsync(p => p.Code == "user.view");
        var userCreatePerm = await db.Permissions.FirstAsync(p => p.Code == "user.create");

        if (!await db.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == userUpdatePerm.Id))
            await db.RolePermissions.AddAsync(new RolePermission { RoleId = adminRole.Id, PermissionId = userUpdatePerm.Id });
        if (!await db.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == userViewPerm.Id))
            await db.RolePermissions.AddAsync(new RolePermission { RoleId = adminRole.Id, PermissionId = userViewPerm.Id });
        if (!await db.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == userCreatePerm.Id))
            await db.RolePermissions.AddAsync(new RolePermission { RoleId = adminRole.Id, PermissionId = userCreatePerm.Id });
        await db.SaveChangesAsync();

        // Create div1admin user
        var adminUserCreate = await sysAdminClient.PostAsJsonAsync("/api/users", new CreateUserCommand(
            "div1admin",
            "div1admin@oneaccess.local",
            "AdminPass123!",
            "Division 1 Admin",
            div1.Id,
            null,
            new[] { adminRole.Id }
        ));
        var adminUserObj = await adminUserCreate.Content.ReadFromJsonAsync<CreateUserResponse>(_jsonOptions);

        await db.UserDivisionAssignments.AddAsync(new UserDivisionAssignment
        {
            UserId = adminUserObj!.Id,
            DivisionId = div1.Id,
            GrantedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Create a target user in Div 1
        var targetUserRes = await sysAdminClient.PostAsJsonAsync("/api/users", new CreateUserCommand(
            "targetuser",
            "target@oneaccess.local",
            "Password123!",
            "Target User",
            div1.Id
        ));
        var targetUserObj = await targetUserRes.Content.ReadFromJsonAsync<CreateUserResponse>(_jsonOptions);

        // Login as div1admin
        var adminClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
        await adminClient.PostAsJsonAsync("/api/auth/login", new LoginCommand("div1admin", "AdminPass123!"));

        var assignSysRolePayload = new { roleId = sysAdminRoleId };
        var assignSysRoleJson = JsonSerializer.Serialize(assignSysRolePayload, _jsonOptions);
        _output.WriteLine($"Request (as div1admin): POST /api/users/{targetUserObj!.Id}/roles\n{assignSysRoleJson}");
        var nonSysAdminAssignSysRoleRes = await adminClient.PostAsJsonAsync($"/api/users/{targetUserObj.Id}/roles", assignSysRolePayload);
        var nonSysAdminAssignSysRoleContent = await nonSysAdminAssignSysRoleRes.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {(int)nonSysAdminAssignSysRoleRes.StatusCode} {nonSysAdminAssignSysRoleRes.StatusCode}\n{nonSysAdminAssignSysRoleContent}\n");
        nonSysAdminAssignSysRoleRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // -------------------------------------------------------------
        // Scenario 5: Create a user with a DivisionId/SectionId mismatch -> rejected (400)
        // -------------------------------------------------------------
        _output.WriteLine("### Scenario 5: Create user with DivisionId/SectionId mismatch");
        var mismatchPayload = new CreateUserCommand(
            "mismatchuser",
            "mismatch@oneaccess.local",
            "Password123!",
            "Mismatch User",
            div1.Id,  // Division 1
            sec2.Id   // Belongs to Division 2
        );
        var mismatchJson = JsonSerializer.Serialize(mismatchPayload, _jsonOptions);
        _output.WriteLine($"Request: POST /api/users\n{mismatchJson}");
        var mismatchCreateResponse = await sysAdminClient.PostAsJsonAsync("/api/users", mismatchPayload);
        var mismatchContent = await mismatchCreateResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {(int)mismatchCreateResponse.StatusCode} {mismatchCreateResponse.StatusCode}\n{mismatchContent}\n");
        mismatchCreateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // -------------------------------------------------------------
        // Scenario 6: As a non-SysAdmin Administrator, attempt to create/update a user outside assigned Division -> rejected with 403
        // -------------------------------------------------------------
        _output.WriteLine("### Scenario 6: Non-SysAdmin Administrator attempts to create user in unmanaged Division");
        var outOfScopePayload = new CreateUserCommand(
            "outofscopeuser",
            "outofscope@oneaccess.local",
            "Password123!",
            "Out of Scope User",
            div2.Id // Div 2 is unmanaged by div1admin
        );
        var outOfScopeJson = JsonSerializer.Serialize(outOfScopePayload, _jsonOptions);
        _output.WriteLine($"Request (as div1admin): POST /api/users\n{outOfScopeJson}");
        var createOutOfScopeRes = await adminClient.PostAsJsonAsync("/api/users", outOfScopePayload);
        var createOutOfScopeContent = await createOutOfScopeRes.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {(int)createOutOfScopeRes.StatusCode} {createOutOfScopeRes.StatusCode}\n{createOutOfScopeContent}\n");
        createOutOfScopeRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // -------------------------------------------------------------
        // Scenario 7: Successfully create a second System Administrator via AssignRole,
        // then confirm GET /api/auth/me for THAT user shows isSystemAdministrator: true and the full permission set, without re-login
        // -------------------------------------------------------------
        _output.WriteLine("### Scenario 7: Assign System Administrator role and verify live permission elevation via GET /api/auth/me without re-login");
        var secondUserCreateRes = await sysAdminClient.PostAsJsonAsync("/api/users", new CreateUserCommand(
            "secondadmin",
            "secondadmin@oneaccess.local",
            "SecondAdmin123!",
            "Second Administrator"
        ));
        var secondUserObj = await secondUserCreateRes.Content.ReadFromJsonAsync<CreateUserResponse>(_jsonOptions);

        var secondAdminClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
        await secondAdminClient.PostAsJsonAsync("/api/auth/login", new LoginCommand("secondadmin", "SecondAdmin123!"));

        _output.WriteLine("Step 7a: GET /api/auth/me for secondadmin BEFORE role assignment:");
        var meBeforeRes = await secondAdminClient.GetAsync("/api/auth/me");
        var meBeforeContent = await meBeforeRes.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {(int)meBeforeRes.StatusCode} {meBeforeRes.StatusCode}\n{meBeforeContent}\n");

        _output.WriteLine($"Step 7b: Root SysAdmin assigns 'System Administrator' role to secondadmin:");
        var assignSysAdminRes = await sysAdminClient.PostAsJsonAsync($"/api/users/{secondUserObj!.Id}/roles", new { roleId = sysAdminRoleId });
        var assignSysAdminContent = await assignSysAdminRes.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {(int)assignSysAdminRes.StatusCode} {assignSysAdminRes.StatusCode}\n{assignSysAdminContent}\n");
        assignSysAdminRes.StatusCode.Should().Be(HttpStatusCode.OK);

        _output.WriteLine("Step 7c: GET /api/auth/me for secondadmin AFTER role assignment (using original session cookie, WITHOUT re-login):");
        var meAfterRes = await secondAdminClient.GetAsync("/api/auth/me");
        var meAfterContent = await meAfterRes.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {(int)meAfterRes.StatusCode} {meAfterRes.StatusCode}\n{meAfterContent}\n");

        var meAfter = JsonSerializer.Deserialize<CurrentUserResponse>(meAfterContent, _jsonOptions);
        meAfter.Should().NotBeNull();
        meAfter!.IsSystemAdministrator.Should().BeTrue();
        meAfter.Roles.Should().Contain(SystemRoles.SystemAdministrator);
        meAfter.Permissions.Should().Contain(new[] { "user.create", "user.update", "role.create", "role.delete", "audit.view" });
    }
}
