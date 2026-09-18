using OneAccess.Domain.Entities;

namespace OneAccess.Application.Common.Interfaces;

/// <summary>
/// Read-only database query context for CQRS query handlers and scope resolution.
/// No EF Core or DbContext types leak across this boundary.
/// </summary>
public interface IReadDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<Role> Roles { get; }
    IQueryable<Permission> Permissions { get; }
    IQueryable<UserRole> UserRoles { get; }
    IQueryable<RolePermission> RolePermissions { get; }
    IQueryable<SubSystem> SubSystems { get; }
    IQueryable<UserSubSystemAccess> UserSubSystemAccesses { get; }
    IQueryable<RoleSubSystemAccess> RoleSubSystemAccesses { get; }
    IQueryable<Division> Divisions { get; }
    IQueryable<Section> Sections { get; }
    IQueryable<UserDivisionAssignment> UserDivisionAssignments { get; }
    IQueryable<RefreshToken> RefreshTokens { get; }
    IQueryable<SetupCode> SetupCodes { get; }
    IQueryable<AuditLog> AuditLogs { get; }
}
