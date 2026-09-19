using Microsoft.EntityFrameworkCore;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;

namespace OneAccess.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds default permissions, roles, and role-permission mappings.
/// </summary>
public static class PermissionSeeder
{
    public static readonly (string Code, string Module, string Description, bool IsDelegable)[] AllPermissions = new[]
    {
        ("user.view", "Users", "View users", true),
        ("user.create", "Users", "Create new users", true),
        ("user.update", "Users", "Update existing users and assign roles", true),
        ("user.delete", "Users", "Deactivate/delete users", true),

        ("role.view", "Roles", "View roles", true),
        ("role.create", "Roles", "Create new roles", false),
        ("role.update", "Roles", "Update existing roles", false),
        ("role.delete", "Roles", "Delete roles", false),

        ("permission.view", "Permissions", "View all available permissions", true),

        ("rolepermission.assign", "RolePermissions", "Assign permissions to roles", false),
        ("rolepermission.revoke", "RolePermissions", "Revoke permissions from roles", false),

        ("subsystem.view", "SubSystems", "View registered sub-systems", true),
        ("subsystem.register", "SubSystems", "Register new sub-systems", false),
        ("subsystem.update", "SubSystems", "Update sub-system configurations", false),

        ("rolesubsystem.assign", "RoleSubSystemAccess", "Add sub-system to role allow-list", false),
        ("rolesubsystem.revoke", "RoleSubSystemAccess", "Remove sub-system from role allow-list", false),

        ("usersubsystem.assign", "UserSubSystemAccess", "Add sub-system to user override allow-list", false),
        ("usersubsystem.revoke", "UserSubSystemAccess", "Remove sub-system from user override allow-list", false),

        ("division.view", "Divisions", "View divisions", true),
        ("division.create", "Divisions", "Create divisions", false),
        ("division.update", "Divisions", "Update divisions", false),
        ("division.delete", "Divisions", "Delete divisions", false),

        ("section.view", "Sections", "View sections", true),
        ("section.create", "Sections", "Create sections", true),
        ("section.update", "Sections", "Update sections", true),
        ("section.delete", "Sections", "Delete sections", true),

        ("userdivision.assign", "UserDivisionAssignment", "Assign administrator to manage division", false),
        ("userdivision.revoke", "UserDivisionAssignment", "Revoke administrator from managing division", false),

        ("audit.view", "Audit", "View audit trail logs (System Administrator only)", false)
    };

    public static async Task SeedAsync(OneAccessDbContext context, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // 1. Seed Permissions
        var existingPerms = await context.Permissions.ToDictionaryAsync(p => p.Code, ct);
        var permissionsToAdd = new List<Permission>();

        foreach (var def in AllPermissions)
        {
            if (!existingPerms.TryGetValue(def.Code, out var perm))
            {
                permissionsToAdd.Add(new Permission
                {
                    Code = def.Code,
                    Module = def.Module,
                    Description = def.Description,
                    IsDelegable = def.IsDelegable
                });
            }
            else
            {
                perm.Module = def.Module;
                perm.Description = def.Description;
                perm.IsDelegable = def.IsDelegable;
            }
        }

        if (permissionsToAdd.Count != 0)
        {
            await context.Permissions.AddRangeAsync(permissionsToAdd, ct);
        }

        await context.SaveChangesAsync(ct);

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
