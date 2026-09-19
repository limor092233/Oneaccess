using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneAccess.API.Endpoints;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;
using OneAccess.Application.Features.Audit.Queries.GetAuditLogs;
using OneAccess.Application.Features.Auth.Commands.Login;
using OneAccess.Application.Features.Divisions.Commands.AssignUserDivision;
using OneAccess.Application.Features.Divisions.Commands.CreateDivision;
using OneAccess.Application.Features.Permissions.Queries.GetPermissions;
using OneAccess.Application.Features.Roles.Commands.UpdateRole;
using OneAccess.Application.Features.Setup.Commands.InitializeSystemAdmin;
using OneAccess.Application.Features.Users.Commands.CreateUser;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using OneAccess.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace OneAccess.API.IntegrationTests;

public class Step5dLiveVerificationTests : IClassFixture<OneAccessApiFactory>
{
    private readonly OneAccessApiFactory _factory;
    private readonly ITestOutputHelper _output;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public Step5dLiveVerificationTests(OneAccessApiFactory factory, ITestOutputHelper output)
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
                "rootadmin5d",
                "root5d@oneaccess.local",
                "RootAdmin123!",
                "Root Administrator 5d"
            ));
        }

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(
            "rootadmin5d",
            "RootAdmin123!"
        ));
        loginResponse.EnsureSuccessStatusCode();

        var adminUser = await db.Users.FirstAsync(u => u.Username == "rootadmin5d");
        return (client, adminUser.Id);
    }

    [Fact]
    public async Task Run_And_Verify_All_Step5d_Scenarios()
    {
        var (sysAdminClient, sysAdminUserId) = await SetupAndLoginRootSysAdminAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OneAccessDbContext>();

        // Ensure we perform some actions to guarantee audit log entries in this session
        var userRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.User);
        var adminRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.Administrator);

        // Action 1: Create a Division
        var divResp = await sysAdminClient.PostAsJsonAsync("/api/divisions", new CreateDivisionCommand("Div5d", "Division 5d"));
        divResp.EnsureSuccessStatusCode();
        var div = await divResp.Content.ReadFromJsonAsync<OneAccess.Application.Features.Divisions.Commands.CreateDivision.CreateDivisionResponse>(_jsonOptions);

        // Action 2: Create div1admin user with Administrator role
        var createDivAdminResp = await sysAdminClient.PostAsJsonAsync("/api/users", new CreateUserCommand(
            "div1admin5d",
            "div1admin5d@oneaccess.local",
            "DivAdmin123!",
            "Division 1 Admin 5d",
            div!.Id,
            null,
            new List<Guid> { adminRole.Id }
        ));
        createDivAdminResp.EnsureSuccessStatusCode();
        var divAdminUser = await createDivAdminResp.Content.ReadFromJsonAsync<OneAccess.Application.Features.Users.Commands.CreateUser.CreateUserResponse>(_jsonOptions);

        // Action 3: Assign div1admin to manage division
        var assignDivResp = await sysAdminClient.PostAsJsonAsync($"/api/divisions/{div.Id}/users", new AssignUserDivisionRequest(divAdminUser!.Id));
        assignDivResp.EnsureSuccessStatusCode();

        // Action 4: Create a custom role and update it (writes role_created and role_updated to AuditLog)
        var createRoleResp = await sysAdminClient.PostAsJsonAsync("/api/roles", new OneAccess.Application.Features.Roles.Commands.CreateRole.CreateRoleCommand(
            "AuditedRole5d",
            "Initial Description"
        ));
        createRoleResp.EnsureSuccessStatusCode();
        var createdRole = await createRoleResp.Content.ReadFromJsonAsync<OneAccess.Application.Features.Roles.Commands.CreateRole.CreateRoleResponse>(_jsonOptions);

        var updateRoleResp = await sysAdminClient.PutAsJsonAsync($"/api/roles/{createdRole!.Id}", new UpdateRoleRequest(
            "AuditedRole5d",
            "Updated Description"
        ));
        updateRoleResp.EnsureSuccessStatusCode();

        // =========================================================================
        // SCENARIO 1: GET /api/permissions as System Administrator -> paste response, confirm IsDelegable present
        // =========================================================================
        _output.WriteLine("=== SCENARIO 1: GET /api/permissions as System Administrator ===");
        var permResp = await sysAdminClient.GetAsync("/api/permissions");
        var permContent = await permResp.Content.ReadAsStringAsync();
        _output.WriteLine($"GET /api/permissions -> Status: {(int)permResp.StatusCode} {permResp.StatusCode}\nResponse: {permContent}");
        permResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var permissions = await permResp.Content.ReadFromJsonAsync<List<PermissionDto>>(_jsonOptions);
        permissions.Should().NotBeNull();
        permissions.Should().NotBeEmpty();

        var delegablePerm = permissions!.FirstOrDefault(p => p.Code == "permission.view");
        delegablePerm.Should().NotBeNull();
        delegablePerm!.IsDelegable.Should().BeTrue();

        var nonDelegablePerm = permissions.FirstOrDefault(p => p.Code == "audit.view");
        nonDelegablePerm.Should().NotBeNull();
        nonDelegablePerm!.IsDelegable.Should().BeFalse();

        // =========================================================================
        // SCENARIO 2: GET /api/audit as System Administrator, unfiltered -> confirm real historical entries
        // =========================================================================
        _output.WriteLine("\n=== SCENARIO 2: GET /api/audit as System Administrator (unfiltered) ===");
        var auditUnfilteredResp = await sysAdminClient.GetAsync("/api/audit");
        var auditUnfilteredContent = await auditUnfilteredResp.Content.ReadAsStringAsync();
        _output.WriteLine($"GET /api/audit -> Status: {(int)auditUnfilteredResp.StatusCode} {auditUnfilteredResp.StatusCode}\nResponse: {auditUnfilteredContent}");
        auditUnfilteredResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedAuditLogs = await auditUnfilteredResp.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>(_jsonOptions);
        pagedAuditLogs.Should().NotBeNull();
        pagedAuditLogs!.TotalCount.Should().BeGreaterThan(0);
        pagedAuditLogs.Items.Should().NotBeEmpty();
        pagedAuditLogs.Items.Should().AllSatisfy(item =>
        {
            item.Id.Should().NotBeEmpty();
            item.Action.Should().NotBeNullOrWhiteSpace();
            item.EntityType.Should().NotBeNullOrWhiteSpace();
            item.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddMinutes(1));
        });

        // =========================================================================
        // SCENARIO 3: GET /api/audit filtered by EntityType=Role and a date range
        // =========================================================================
        _output.WriteLine("\n=== SCENARIO 3: GET /api/audit filtered by EntityType=Role and date range ===");
        var fromUtc = DateTime.UtcNow.AddHours(-1).ToString("O");
        var toUtc = DateTime.UtcNow.AddHours(1).ToString("O");
        var filterUrl = $"/api/audit?entityType=Role&fromUtc={Uri.EscapeDataString(fromUtc)}&toUtc={Uri.EscapeDataString(toUtc)}";
        var auditFilteredResp = await sysAdminClient.GetAsync(filterUrl);
        var auditFilteredContent = await auditFilteredResp.Content.ReadAsStringAsync();
        _output.WriteLine($"GET {filterUrl} -> Status: {(int)auditFilteredResp.StatusCode} {auditFilteredResp.StatusCode}\nResponse: {auditFilteredContent}");
        auditFilteredResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var filteredAuditLogs = await auditFilteredResp.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>(_jsonOptions);
        filteredAuditLogs.Should().NotBeNull();
        filteredAuditLogs!.Items.Should().NotBeEmpty();
        filteredAuditLogs.Items.Should().OnlyContain(item => item.EntityType == "Role");

        // =========================================================================
        // SCENARIO 4: Attempt GET /api/audit as div1admin (no audit.view) -> 403 RFC 7807 shape
        // =========================================================================
        _output.WriteLine("\n=== SCENARIO 4: GET /api/audit as div1admin (no audit.view) ===");
        var divAdminClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
        var divAdminLoginResp = await divAdminClient.PostAsJsonAsync("/api/auth/login", new LoginCommand("div1admin5d", "DivAdmin123!"));
        divAdminLoginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var divAdminAuditResp = await divAdminClient.GetAsync("/api/audit");
        var divAdminAuditContent = await divAdminAuditResp.Content.ReadAsStringAsync();
        _output.WriteLine($"GET /api/audit (div1admin5d) -> Status: {(int)divAdminAuditResp.StatusCode} {divAdminAuditResp.StatusCode}\nResponse: {divAdminAuditContent}");
        divAdminAuditResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        divAdminAuditContent.Should().Contain("Forbidden");
        divAdminAuditContent.Should().Contain("403");

        // =========================================================================
        // SCENARIO 5: GET /api/permissions as div1admin (holds permission.view) -> 200 OK
        // =========================================================================
        _output.WriteLine("\n=== SCENARIO 5: GET /api/permissions as div1admin (holds permission.view) ===");
        var divAdminPermResp = await divAdminClient.GetAsync("/api/permissions");
        var divAdminPermContent = await divAdminPermResp.Content.ReadAsStringAsync();
        _output.WriteLine($"GET /api/permissions (div1admin5d) -> Status: {(int)divAdminPermResp.StatusCode} {divAdminPermResp.StatusCode}\nResponse: {divAdminPermContent}");
        divAdminPermResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var divAdminPerms = await divAdminPermResp.Content.ReadFromJsonAsync<List<PermissionDto>>(_jsonOptions);
        divAdminPerms.Should().NotBeNull();
        divAdminPerms.Should().NotBeEmpty();
        divAdminPerms!.Count.Should().Be(permissions.Count);
    }
}
