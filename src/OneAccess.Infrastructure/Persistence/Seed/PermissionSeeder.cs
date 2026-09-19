using Microsoft.EntityFrameworkCore;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;

namespace OneAccess.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds default permissions, roles, and role-permission mappings.
/// </summary>
public static class PermissionSeeder
{
    public static readonly (string Code, string Module, string Description)[] AllPermissions = new[]
    {
        ("user.view", "Users", "View users"),
        ("user.create", "Users", "Create new users"),
        ("user.update", "Users", "Update existing users and assign roles"),
        ("user.delete", "Users", "Deactivate/delete users"),

        ("role.view", "Roles", "View roles"),
        ("role.create", "Roles", "Create new roles"),
        ("role.update", "Roles", "Update existing roles"),
        ("role.delete", "Roles", "Delete roles"),

        ("permission.view", "Permissions", "View all available permissions"),

        ("rolepermission.assign", "RolePermissions", "Assign permissions to roles"),
        ("rolepermission.revoke", "RolePermissions", "Revoke permissions from roles"),

        ("subsystem.view", "SubSystems", "View registered sub-systems"),
        ("subsystem.register", "SubSystems", "Register new sub-systems"),
        ("subsystem.update", "SubSystems", "Update sub-system configurations"),

        ("rolesubsystem.assign", "RoleSubSystemAccess", "Add sub-system to role allow-list"),
        ("rolesubsystem.revoke", "RoleSubSystemAccess", "Remove sub-system from role allow-list"),

        ("usersubsystem.assign", "UserSubSystemAccess", "Add sub-system to user override allow-list"),
        ("usersubsystem.revoke", "UserSubSystemAccess", "Remove sub-system from user override allow-list"),

        ("division.view", "Divisions", "View divisions"),
        ("division.create", "Divisions", "Create divisions"),
        ("division.update", "Divisions", "Update divisions"),
        ("division.delete", "Divisions", "Delete divisions"),

        ("section.view", "Sections", "View sections"),
        ("section.create", "Sections", "Create sections"),
        ("section.update", "Sections", "Update sections"),
        ("section.delete", "Sections", "Delete sections"),

        ("userdivision.assign", "UserDivisionAssignment", "Assign administrator to manage division"),
        ("userdivision.revoke", "UserDivisionAssignment", "Revoke administrator from managing division"),

        ("audit.view", "Audit", "View audit trail logs (System Administrator only)")
    };

    public static async Task SeedAsync(OneAccessDbContext context, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // 1. Seed Permissions
        var existingCodes = await context.Permissions.Select(p => p.Code).ToListAsync(ct);
        var permissionsToAdd = AllPermissions
            .Where(p => !existingCodes.Contains(p.Code))
            .Select(p => new Permission
            {
                Code = p.Code,
                Module = p.Module,
                Description = p.Description
            })
            .ToList();

        if (permissionsToAdd.Count != 0)
        {
            await context.Permissions.AddRangeAsync(permissionsToAdd, ct);
            await context.SaveChangesAsync(ct);
        }

        // 2. Seed Default Roles
        var existingRoles = await context.Roles.ToDictionaryAsync(r => r.Name, ct);

        if (!existingRoles.ContainsKey(SystemRoles.SystemAdministrator))
        {
            var sysAdminRole = new Role
            {
                Name = SystemRoles.SystemAdministrator,
                Description = "Full unrestricted system control.",
                IsSystemRole = true,
                CreatedAt = now
            };
            context.Roles.Add(sysAdminRole);
            existingRoles[sysAdminRole.Name] = sysAdminRole;
        }

        if (!existingRoles.ContainsKey(SystemRoles.Administrator))
        {
            var adminRole = new Role
            {
                Name = SystemRoles.Administrator,
                Description = "Division-scoped administrator.",
                IsSystemRole = true,
                CreatedAt = now
            };
            context.Roles.Add(adminRole);
            existingRoles[adminRole.Name] = adminRole;
        }

        if (!existingRoles.ContainsKey(SystemRoles.User))
        {
            var userRole = new Role
            {
                Name = SystemRoles.User,
                Description = "Standard portal user.",
                IsSystemRole = true,
                CreatedAt = now
            };
            context.Roles.Add(userRole);
            existingRoles[userRole.Name] = userRole;
        }

        await context.SaveChangesAsync(ct);

        // 3. Seed Role Permissions for System Administrator (all permissions)
        var sysAdmin = existingRoles[SystemRoles.SystemAdministrator];
        var allDbPermissions = await context.Permissions.ToListAsync(ct);
        var existingSysAdminPerms = await context.RolePermissions
            .Where(rp => rp.RoleId == sysAdmin.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);

        var sysAdminPermsToAdd = allDbPermissions
            .Where(p => !existingSysAdminPerms.Contains(p.Id))
            .Select(p => new RolePermission
            {
                RoleId = sysAdmin.Id,
                PermissionId = p.Id
            })
            .ToList();

        if (sysAdminPermsToAdd.Count != 0)
        {
            await context.RolePermissions.AddRangeAsync(sysAdminPermsToAdd, ct);
        }

        // 4. Seed Role Permissions for Administrator (delegable division/user/section management)
        var admin = existingRoles[SystemRoles.Administrator];
        var adminPermCodes = new HashSet<string>
        {
            "user.view", "user.create", "user.update", "user.delete",
            "role.view", "permission.view",
            "division.view",
            "section.view", "section.create", "section.update", "section.delete",
            "subsystem.view"
        };
        var existingAdminPerms = await context.RolePermissions
            .Where(rp => rp.RoleId == admin.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);

        var adminPermsToAdd = allDbPermissions
            .Where(p => adminPermCodes.Contains(p.Code) && !existingAdminPerms.Contains(p.Id))
            .Select(p => new RolePermission
            {
                RoleId = admin.Id,
                PermissionId = p.Id
            })
            .ToList();

        if (adminPermsToAdd.Count != 0)
        {
            await context.RolePermissions.AddRangeAsync(adminPermsToAdd, ct);
        }

        // 5. Seed Role Permissions for User (basic portal view)
        var standardUser = existingRoles[SystemRoles.User];
        var userPermCodes = new HashSet<string>
        {
            "subsystem.view"
        };
        var existingUserPerms = await context.RolePermissions
            .Where(rp => rp.RoleId == standardUser.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);

        var userPermsToAdd = allDbPermissions
            .Where(p => userPermCodes.Contains(p.Code) && !existingUserPerms.Contains(p.Id))
            .Select(p => new RolePermission
            {
                RoleId = standardUser.Id,
                PermissionId = p.Id
            })
            .ToList();

        if (userPermsToAdd.Count != 0)
        {
            await context.RolePermissions.AddRangeAsync(userPermsToAdd, ct);
        }

        await context.SaveChangesAsync(ct);
    }
}
