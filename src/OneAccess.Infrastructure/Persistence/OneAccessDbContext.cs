using Microsoft.EntityFrameworkCore;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Domain.Entities;

namespace OneAccess.Infrastructure.Persistence;

/// <summary>
/// Primary EF Core DbContext for OneAccess. Implements IReadDbContext for read queries.
/// </summary>
public class OneAccessDbContext : DbContext, IReadDbContext
{
    public OneAccessDbContext(DbContextOptions<OneAccessDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SubSystem> SubSystems => Set<SubSystem>();
    public DbSet<UserSubSystemAccess> UserSubSystemAccesses => Set<UserSubSystemAccess>();
    public DbSet<RoleSubSystemAccess> RoleSubSystemAccesses => Set<RoleSubSystemAccess>();
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<UserDivisionAssignment> UserDivisionAssignments => Set<UserDivisionAssignment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SetupCode> SetupCodes => Set<SetupCode>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // IReadDbContext interface implementations (IQueryable<T> as no-tracking)
    IQueryable<User> IReadDbContext.Users => Users.AsNoTracking();
    IQueryable<Role> IReadDbContext.Roles => Roles.AsNoTracking();
    IQueryable<Permission> IReadDbContext.Permissions => Permissions.AsNoTracking();
    IQueryable<UserRole> IReadDbContext.UserRoles => UserRoles.AsNoTracking();
    IQueryable<RolePermission> IReadDbContext.RolePermissions => RolePermissions.AsNoTracking();
    IQueryable<SubSystem> IReadDbContext.SubSystems => SubSystems.AsNoTracking();
    IQueryable<UserSubSystemAccess> IReadDbContext.UserSubSystemAccesses => UserSubSystemAccesses.AsNoTracking();
    IQueryable<RoleSubSystemAccess> IReadDbContext.RoleSubSystemAccesses => RoleSubSystemAccesses.AsNoTracking();
    IQueryable<Division> IReadDbContext.Divisions => Divisions.AsNoTracking();
    IQueryable<Section> IReadDbContext.Sections => Sections.AsNoTracking();
    IQueryable<UserDivisionAssignment> IReadDbContext.UserDivisionAssignments => UserDivisionAssignments.AsNoTracking();
    IQueryable<RefreshToken> IReadDbContext.RefreshTokens => RefreshTokens.AsNoTracking();
    IQueryable<SetupCode> IReadDbContext.SetupCodes => SetupCodes.AsNoTracking();
    IQueryable<AuditLog> IReadDbContext.AuditLogs => AuditLogs.AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OneAccessDbContext).Assembly);
    }
}
