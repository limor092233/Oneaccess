using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneAccess.API.Endpoints;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Auth.Commands.Login;
using OneAccess.Application.Features.RolePermissions.Commands.AssignPermission;
using OneAccess.Application.Features.Setup.Commands.InitializeSystemAdmin;
using OneAccess.Application.Features.SubSystems.Commands.AssignRoleSubSystemAccess;
using OneAccess.Application.Features.SubSystems.Commands.AssignUserSubSystemAccess;
using OneAccess.Application.Features.SubSystems.Commands.RegisterSubSystem;
using OneAccess.Application.Features.SubSystems.Commands.UpdateSubSystem;
using OneAccess.Application.Features.SubSystems.Queries.GetSubSystems;
using OneAccess.Application.Features.Users.Commands.CreateUser;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using OneAccess.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace OneAccess.API.IntegrationTests;

public class Step5cLiveVerificationTests : IClassFixture<OneAccessApiFactory>
{
    private readonly OneAccessApiFactory _factory;
    private readonly ITestOutputHelper _output;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public Step5cLiveVerificationTests(OneAccessApiFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    private async Task<(HttpClient Client, Guid SysAdminUserId)> SetupAndLoginRootSysAdminAsync()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });

        using var scope = _factory.Services.CreateScope();
        var setupCodeService = scope.ServiceProvider.GetRequiredService<ISetupCodeService>();
        var db = scope.ServiceProvider.GetRequiredService<OneAccessDbContext>();

        var existingAdmin = await db.Users.FirstOrDefaultAsync(u => u.IsSystemAdministrator);
        if (existingAdmin == null)
        {
            var code = await setupCodeService.GenerateAndStoreCodeAsync();
            await client.PostAsJsonAsync("/api/setup/initialize", new InitializeSystemAdminCommand(
                code,
                "rootadmin5c",
                "root5c@oneaccess.local",
                "RootAdmin123!",
                "Root Administrator 5c"
            ));
        }

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(
            "rootadmin5c",
            "RootAdmin123!"
        ));
        loginResponse.EnsureSuccessStatusCode();

        var adminUser = await db.Users.FirstAsync(u => u.Username == "rootadmin5c");
        return (client, adminUser.Id);
    }

    [Fact]
    public async Task Run_And_Verify_All_Step5c_Scenarios()
    {
        var (sysAdminClient, sysAdminUserId) = await SetupAndLoginRootSysAdminAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OneAccessDbContext>();

        // =========================================================================
        // SCENARIO 1: Register sub-systems as System Administrator -> success
        // =========================================================================
        _output.WriteLine("=== SCENARIO 1: Register Sub-systems ===");
        var reg1Resp = await sysAdminClient.PostAsJsonAsync("/api/subsystems", new RegisterSubSystemCommand(
            "SYS1",
            "System One CRM",
            "https://crm.local",
            "crm.api",
            true
        ));
        var reg1Content = await reg1Resp.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /api/subsystems (SYS1) -> Status: {(int)reg1Resp.StatusCode} {reg1Resp.StatusCode}\nResponse: {reg1Content}");
        reg1Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var sys1 = await reg1Resp.Content.ReadFromJsonAsync<RegisterSubSystemResponse>(_jsonOptions);
        sys1.Should().NotBeNull();

        var reg2Resp = await sysAdminClient.PostAsJsonAsync("/api/subsystems", new RegisterSubSystemCommand(
            "SYS2",
            "System Two ERP",
            "https://erp.local",
            "erp.api",
            true
        ));
        var reg2Content = await reg2Resp.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /api/subsystems (SYS2) -> Status: {(int)reg2Resp.StatusCode} {reg2Resp.StatusCode}\nResponse: {reg2Content}");
        reg2Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var sys2 = await reg2Resp.Content.ReadFromJsonAsync<RegisterSubSystemResponse>(_jsonOptions);
        sys2.Should().NotBeNull();

        var reg3Resp = await sysAdminClient.PostAsJsonAsync("/api/subsystems", new RegisterSubSystemCommand(
            "SYS3",
            "System Three HR",
            "https://hr.local",
            "hr.api",
            true
        ));
        var reg3Content = await reg3Resp.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /api/subsystems (SYS3) -> Status: {(int)reg3Resp.StatusCode} {reg3Resp.StatusCode}\nResponse: {reg3Content}");
        reg3Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var sys3 = await reg3Resp.Content.ReadFromJsonAsync<RegisterSubSystemResponse>(_jsonOptions);
        sys3.Should().NotBeNull();

        // Also test admin list GET /api/subsystems
        var adminListResp = await sysAdminClient.GetAsync("/api/subsystems");
        var adminListContent = await adminListResp.Content.ReadAsStringAsync();
        _output.WriteLine($"GET /api/subsystems (Admin list) -> Status: {(int)adminListResp.StatusCode} {adminListResp.StatusCode}\nResponse: {adminListContent}");
        adminListResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var allSubSystems = await adminListResp.Content.ReadFromJsonAsync<List<SubSystemDto>>(_jsonOptions);
        allSubSystems.Should().Contain(s => s.Code == "SYS1");
        allSubSystems.Should().Contain(s => s.Code == "SYS2");
        allSubSystems.Should().Contain(s => s.Code == "SYS3");

        // =========================================================================
        // SCENARIO 2: Attempt AssignRoleSubSystemAccess targeting System Administrator -> rejected
        // =========================================================================
        _output.WriteLine("\n=== SCENARIO 2: AssignRoleSubSystemAccess targeting System Administrator ===");
        var sysAdminRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.SystemAdministrator);
        var assignSysAdminResp = await sysAdminClient.PostAsJsonAsync($"/api/roles/{sysAdminRole.Id}/subsystems", new AssignRoleSubSystemRequest(sys1!.Id));
        var assignSysAdminContent = await assignSysAdminResp.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /api/roles/{sysAdminRole.Id}/subsystems -> Status: {(int)assignSysAdminResp.StatusCode} {assignSysAdminResp.StatusCode}\nResponse: {assignSysAdminContent}");
        assignSysAdminResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        assignSysAdminContent.Should().Contain("SystemAdminSubSystemRestrictionNotAllowed");

        // =========================================================================
        // SCENARIO 3: Set RoleSubSystemAccess on 'User' role to single sub-system (SYS1),
        // confirm GET /api/subsystems/mine for User-role account reflects exactly that restriction
        // =========================================================================
        _output.WriteLine("\n=== SCENARIO 3: RoleSubSystemAccess restriction on 'User' role ===");
        var userRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.User);

        // Create user 1 with 'User' role
        var createUser1Resp = await sysAdminClient.PostAsJsonAsync("/api/users", new CreateUserCommand(
            "standarduser5c",
            "standarduser5c@oneaccess.local",
            "UserPass123!",
            "Standard User 5c",
            null,
            null,
            new List<Guid> { userRole.Id }
        ));
        createUser1Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var user1 = await createUser1Resp.Content.ReadFromJsonAsync<CreateUserResponse>(_jsonOptions);
        user1.Should().NotBeNull();

        // Assign restriction on User role to SYS1 only
        var assignRoleResp = await sysAdminClient.PostAsJsonAsync($"/api/roles/{userRole.Id}/subsystems", new AssignRoleSubSystemRequest(sys1.Id));
        var assignRoleContent = await assignRoleResp.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /api/roles/{userRole.Id}/subsystems (SYS1) -> Status: {(int)assignRoleResp.StatusCode} {assignRoleResp.StatusCode}\nResponse: {assignRoleContent}");
        assignRoleResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login as standarduser5c
        var user1Client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
        var user1LoginResp = await user1Client.PostAsJsonAsync("/api/auth/login", new LoginCommand("standarduser5c", "UserPass123!"));
        user1LoginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Confirm GET /api/subsystems/mine reflects ONLY SYS1
        var user1MineResp = await user1Client.GetAsync("/api/subsystems/mine");
        var user1MineContent = await user1MineResp.Content.ReadAsStringAsync();
        _output.WriteLine($"GET /api/subsystems/mine (standarduser5c) -> Status: {(int)user1MineResp.StatusCode} {user1MineResp.StatusCode}\nResponse: {user1MineContent}");
        user1MineResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var user1Mine = await user1MineResp.Content.ReadFromJsonAsync<List<SubSystemDto>>(_jsonOptions);
        user1Mine.Should().HaveCount(1);
        user1Mine!.Single().Code.Should().Be("SYS1");

        // =========================================================================
        // SCENARIO 4: Set UserSubSystemAccess override for one specific user that differs
        // from their role's restriction, confirm /mine reflects the override, not role default
        // =========================================================================
        _output.WriteLine("\n=== SCENARIO 4: UserSubSystemAccess override ===");
        // Create user 2 with 'User' role
        var createUser2Resp = await sysAdminClient.PostAsJsonAsync("/api/users", new CreateUserCommand(
            "overrideuser5c",
            "overrideuser5c@oneaccess.local",
            "UserPass123!",
            "Override User 5c",
            null,
            null,
            new List<Guid> { userRole.Id }
        ));
        createUser2Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var user2 = await createUser2Resp.Content.ReadFromJsonAsync<CreateUserResponse>(_jsonOptions);
        user2.Should().NotBeNull();

        // Grant override on user 2 for SYS2 and SYS3 (differing from role's SYS1 restriction)
        var assignUserResp1 = await sysAdminClient.PostAsJsonAsync($"/api/users/{user2!.Id}/subsystems", new AssignUserSubSystemRequest(sys2!.Id));
        assignUserResp1.StatusCode.Should().Be(HttpStatusCode.OK);
        var assignUserResp2 = await sysAdminClient.PostAsJsonAsync($"/api/users/{user2.Id}/subsystems", new AssignUserSubSystemRequest(sys3!.Id));
        assignUserResp2.StatusCode.Should().Be(HttpStatusCode.OK);
        _output.WriteLine($"POST /api/users/{user2.Id}/subsystems -> Added SYS2 and SYS3 override grants");

        // Login as overrideuser5c
        var user2Client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
        var user2LoginResp = await user2Client.PostAsJsonAsync("/api/auth/login", new LoginCommand("overrideuser5c", "UserPass123!"));
        user2LoginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Confirm GET /api/subsystems/mine reflects SYS2 and SYS3 (override beats role restriction)
        var user2MineResp = await user2Client.GetAsync("/api/subsystems/mine");
        var user2MineContent = await user2MineResp.Content.ReadAsStringAsync();
        _output.WriteLine($"GET /api/subsystems/mine (overrideuser5c) -> Status: {(int)user2MineResp.StatusCode} {user2MineResp.StatusCode}\nResponse: {user2MineContent}");
        user2MineResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var user2Mine = await user2MineResp.Content.ReadFromJsonAsync<List<SubSystemDto>>(_jsonOptions);
        user2Mine.Should().HaveCount(2);
        user2Mine!.Select(s => s.Code).Should().BeEquivalentTo(new[] { "SYS2", "SYS3" });

        // =========================================================================
        // SCENARIO 5: Attempt to assign rolesubsystem.assign permission to Administrator role -> rejected (non-delegable)
        // =========================================================================
        _output.WriteLine("\n=== SCENARIO 5: Assign non-delegable rolesubsystem.assign to Administrator role ===");
        var adminRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.Administrator);
        var roleSubsystemAssignPerm = await db.Permissions.FirstAsync(p => p.Code == "rolesubsystem.assign");

        var assignNonDelegableResp = await sysAdminClient.PostAsJsonAsync($"/api/roles/{adminRole.Id}/permissions", new AssignPermissionRequest(roleSubsystemAssignPerm.Id));
        var assignNonDelegableContent = await assignNonDelegableResp.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /api/roles/{adminRole.Id}/permissions (rolesubsystem.assign) -> Status: {(int)assignNonDelegableResp.StatusCode} {assignNonDelegableResp.StatusCode}\nResponse: {assignNonDelegableContent}");
        assignNonDelegableResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        assignNonDelegableContent.Should().Contain("NonDelegablePermission");
    }
}
