using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneAccess.API.Endpoints;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Features.Auth.Commands.Login;
using OneAccess.Application.Features.Divisions.Commands.AssignUserDivision;
using OneAccess.Application.Features.Divisions.Commands.CreateDivision;
using OneAccess.Application.Features.Divisions.Commands.UpdateDivision;
using OneAccess.Application.Features.Divisions.Queries.GetDivisionAdministrators;
using OneAccess.Application.Features.Divisions.Queries.GetDivisions;
using OneAccess.Application.Features.Sections.Commands.CreateSection;
using OneAccess.Application.Features.Sections.Commands.UpdateSection;
using OneAccess.Application.Features.Sections.Queries.GetSectionsByDivision;
using OneAccess.Application.Features.Setup.Commands.InitializeSystemAdmin;
using OneAccess.Application.Features.Users.Commands.AssignRole;
using OneAccess.Application.Features.Users.Commands.CreateUser;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using OneAccess.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace OneAccess.API.IntegrationTests;

public class Step5bLiveVerificationTests : IClassFixture<OneAccessApiFactory>
{
    private readonly OneAccessApiFactory _factory;
    private readonly ITestOutputHelper _output;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public Step5bLiveVerificationTests(OneAccessApiFactory factory, ITestOutputHelper output)
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
                "rootadmin5b",
                "root5b@oneaccess.local",
                "RootAdmin123!",
                "Root Administrator 5b"
            ));
        }

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(
            "rootadmin5b",
            "RootAdmin123!"
        ));
        loginResponse.EnsureSuccessStatusCode();

        var adminUser = await db.Users.FirstAsync(u => u.Username == "rootadmin5b");
        return (client, adminUser.Id);
    }

    [Fact]
    public async Task Run_And_Verify_All_Step5b_Scenarios()
    {
        var (sysAdminClient, sysAdminUserId) = await SetupAndLoginRootSysAdminAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OneAccessDbContext>();

        // Scenario 1: SysAdmin creates Division A (Engineering) and Division B (Operations)
        var createDivAResp = await sysAdminClient.PostAsJsonAsync("/api/divisions", new CreateDivisionCommand(
            "Engineering 5b",
            "Software engineering division"
        ));
        createDivAResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var divA = await createDivAResp.Content.ReadFromJsonAsync<CreateDivisionResponse>(_jsonOptions);
        divA.Should().NotBeNull();
        _output.WriteLine($"[1] Created Division A: {divA!.Id} - {divA.Name}");

        var createDivBResp = await sysAdminClient.PostAsJsonAsync("/api/divisions", new CreateDivisionCommand(
            "Operations 5b",
            "Operations division"
        ));
        createDivBResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var divB = await createDivBResp.Content.ReadFromJsonAsync<CreateDivisionResponse>(_jsonOptions);
        divB.Should().NotBeNull();
        _output.WriteLine($"[1b] Created Division B: {divB!.Id} - {divB.Name}");

        // Scenario 2: SysAdmin creates Section under Division A
        var createSecResp = await sysAdminClient.PostAsJsonAsync("/api/sections", new CreateSectionCommand(
            divA.Id,
            "Core Platform 5b",
            "Core infrastructure section"
        ));
        createSecResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var secA = await createSecResp.Content.ReadFromJsonAsync<CreateSectionResponse>(_jsonOptions);
        secA.Should().NotBeNull();
        _output.WriteLine($"[2] Created Section under Division A: {secA!.Id} - {secA.Name}");

        // Scenario 3: SysAdmin creates an Administrator user and assigns them to manage Division A only
        var adminRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.Administrator);
        var createAdminUserResp = await sysAdminClient.PostAsJsonAsync("/api/users", new CreateUserCommand(
            "engadmin5b",
            "engadmin5b@oneaccess.local",
            "EngAdmin123!",
            "Engineering Admin 5b",
            divA.Id,
            secA.Id,
            new List<Guid> { adminRole.Id }
        ));
        createAdminUserResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var adminUser = await createAdminUserResp.Content.ReadFromJsonAsync<CreateUserResponse>(_jsonOptions);
        adminUser.Should().NotBeNull();

        // Assign Administrator to manage Division A
        var assignDivResp = await sysAdminClient.PostAsJsonAsync($"/api/divisions/{divA.Id}/users", new AssignUserDivisionRequest(adminUser!.Id));
        assignDivResp.StatusCode.Should().Be(HttpStatusCode.OK);
        _output.WriteLine($"[3] Assigned engadmin5b to manage Division A ({divA.Id})");

        // Verify GetDivisionAdministrators
        var getAdminsResp = await sysAdminClient.GetAsync($"/api/divisions/{divA.Id}/users");
        getAdminsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminsList = await getAdminsResp.Content.ReadFromJsonAsync<List<DivisionAdministratorDto>>(_jsonOptions);
        adminsList.Should().Contain(a => a.UserId == adminUser.Id);

        // Scenario 4: Administrator logs in and tests division-scoped visibility
        var adminClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
        var adminLoginResp = await adminClient.PostAsJsonAsync("/api/auth/login", new LoginCommand("engadmin5b", "EngAdmin123!"));
        adminLoginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Non-SysAdmin GET /api/divisions returns only Division A
        var getDivsResp = await adminClient.GetAsync("/api/divisions");
        getDivsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var divsList = await getDivsResp.Content.ReadFromJsonAsync<List<DivisionDto>>(_jsonOptions);
        divsList.Should().Contain(d => d.Id == divA.Id);
        divsList.Should().NotContain(d => d.Id == divB.Id);
        _output.WriteLine($"[4] engadmin5b sees only assigned Division A ({divsList!.Count} total)");

        // Scenario 5: Administrator creates a Section in Division A (scoped within their assignment)
        var createSecInAResp = await adminClient.PostAsJsonAsync("/api/sections", new CreateSectionCommand(
            divA.Id,
            "QA Section 5b",
            "Quality Assurance"
        ));
        createSecInAResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var secInA = await createSecInAResp.Content.ReadFromJsonAsync<CreateSectionResponse>(_jsonOptions);
        _output.WriteLine($"[5] engadmin5b created section in Division A: {secInA!.Id}");

        // Scenario 6: Administrator attempts to create a Section in unassigned Division B -> rejected by DivisionScopeBehavior (403 Forbidden)
        var createSecInBResp = await adminClient.PostAsJsonAsync("/api/sections", new CreateSectionCommand(
            divB.Id,
            "Illegal Section 5b",
            "Should be blocked"
        ));
        createSecInBResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _output.WriteLine($"[6] engadmin5b blocked from creating section in Division B (403 Forbidden)");

        // Scenario 7: Attempting to delete Division A while it has sections and users -> 409 Conflict
        var deleteDivAResp = await sysAdminClient.DeleteAsync($"/api/divisions/{divA.Id}");
        deleteDivAResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _output.WriteLine($"[7] Deleting Division A with active sections/users rejected with 409 Conflict");

        // Scenario 8: Attempting to delete Section A while it is referenced by a user -> 409 Conflict
        var deleteSecAResp = await sysAdminClient.DeleteAsync($"/api/sections/{secA.Id}");
        deleteSecAResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _output.WriteLine($"[8] Deleting Section A referenced by user rejected with 409 Conflict");

        // Scenario 9: Deleting unreferenced Section and Division succeeds
        var deleteSecInAResp = await sysAdminClient.DeleteAsync($"/api/sections/{secInA.Id}");
        deleteSecInAResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteDivBResp = await sysAdminClient.DeleteAsync($"/api/divisions/{divB.Id}");
        deleteDivBResp.StatusCode.Should().Be(HttpStatusCode.OK);
        _output.WriteLine($"[9] Deleting unreferenced section and division B succeeded with 200 OK");

        // Scenario 10: Revoking Administrator assignment
        var revokeResp = await sysAdminClient.DeleteAsync($"/api/divisions/{divA.Id}/users/{adminUser.Id}");
        revokeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        _output.WriteLine($"[10] Revoked engadmin5b management of Division A");

        // engadmin5b now sees 0 divisions
        var getDivsAfterRevokeResp = await adminClient.GetAsync("/api/divisions");
        getDivsAfterRevokeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var divsAfterRevoke = await getDivsAfterRevokeResp.Content.ReadFromJsonAsync<List<DivisionDto>>(_jsonOptions);
        divsAfterRevoke.Should().BeEmpty();
        _output.WriteLine($"[10b] engadmin5b now sees 0 divisions after revocation");
    }

    [Fact]
    public async Task Verify_Requested_Case1_And_Case2_Live_Http()
    {
        var (sysAdminClient, sysAdminUserId) = await SetupAndLoginRootSysAdminAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OneAccessDbContext>();

        // Create Division 1
        var createDivResp = await sysAdminClient.PostAsJsonAsync("/api/divisions", new CreateDivisionCommand(
            "Division 1",
            "Initial Division 1"
        ));
        var div1 = await createDivResp.Content.ReadFromJsonAsync<CreateDivisionResponse>(_jsonOptions);

        // Create Section referencing Division 1
        var createSecResp = await sysAdminClient.PostAsJsonAsync("/api/sections", new CreateSectionCommand(
            div1!.Id,
            "Section 1",
            "Child Section"
        ));
        var sec1 = await createSecResp.Content.ReadFromJsonAsync<CreateSectionResponse>(_jsonOptions);

        // -------------------------------------------------------------
        // Case 1: DELETE Division 1 that still has a Section -> 409
        // -------------------------------------------------------------
        var deleteDivResp = await sysAdminClient.DeleteAsync($"/api/divisions/{div1.Id}");
        var deleteDivBody = await deleteDivResp.Content.ReadAsStringAsync();

        _output.WriteLine("=== CASE 1: DELETE Division with referencing Section ===");
        _output.WriteLine($"REQUEST: DELETE /api/divisions/{div1.Id}");
        _output.WriteLine($"RESPONSE STATUS: {(int)deleteDivResp.StatusCode} {deleteDivResp.StatusCode}");
        _output.WriteLine($"RESPONSE BODY:\n{deleteDivBody}");
        _output.WriteLine("========================================================\n");

        deleteDivResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // -------------------------------------------------------------
        // Setup for Case 2: Create div1admin user and assign to Division 1
        // -------------------------------------------------------------
        var adminRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.Administrator);
        var createAdminUserResp = await sysAdminClient.PostAsJsonAsync("/api/users", new CreateUserCommand(
            "div1admin",
            "div1admin@oneaccess.local",
            "Div1Admin123!",
            "Division 1 Admin",
            div1.Id,
            sec1!.Id,
            new List<Guid> { adminRole.Id }
        ));
        var div1AdminUser = await createAdminUserResp.Content.ReadFromJsonAsync<CreateUserResponse>(_jsonOptions);

        // Assign div1admin to manage Division 1
        await sysAdminClient.PostAsJsonAsync($"/api/divisions/{div1.Id}/users", new AssignUserDivisionRequest(div1AdminUser!.Id));

        // Login as div1admin (non-SysAdmin Administrator holding only ordinary delegated permissions)
        var div1AdminClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
        var loginResp = await div1AdminClient.PostAsJsonAsync("/api/auth/login", new LoginCommand("div1admin", "Div1Admin123!"));
        loginResp.EnsureSuccessStatusCode();

        // -------------------------------------------------------------
        // Case 2: As div1admin, attempt PUT /api/divisions/{own-division-id} -> 403 Forbidden
        // -------------------------------------------------------------
        var updateRequest = new UpdateDivisionRequest("Renamed Division 1", "Attempted by div1admin");
        var updateDivResp = await div1AdminClient.PutAsJsonAsync($"/api/divisions/{div1.Id}", updateRequest);
        var updateDivBody = await updateDivResp.Content.ReadAsStringAsync();

        _output.WriteLine("=== CASE 2: div1admin attempts PUT /api/divisions/{own-division-id} ===");
        _output.WriteLine($"REQUEST: PUT /api/divisions/{div1.Id}");
        _output.WriteLine($"REQUEST BODY:\n{JsonSerializer.Serialize(updateRequest, _jsonOptions)}");
        _output.WriteLine($"RESPONSE STATUS: {(int)updateDivResp.StatusCode} {updateDivResp.StatusCode}");
        _output.WriteLine($"RESPONSE BODY:\n{updateDivBody}");
        _output.WriteLine("======================================================================\n");

        updateDivResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
