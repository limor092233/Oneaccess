namespace OneAccess.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands and queries targeting a division-associated resource.
/// Used by DivisionScopeBehavior to enforce divisional authorization boundaries for non-SysAdmins.
/// </summary>
public interface IDivisionScopedRequest
{
    Task<Guid?> GetTargetDivisionIdAsync(IReadDbContext db, CancellationToken ct = default);
}
