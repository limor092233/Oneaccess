using OneAccess.Domain.Entities;

namespace OneAccess.Application.Common.Interfaces;

/// <summary>
/// Service contract resolving effective sub-system visibility for a user.
/// </summary>
public interface ISubSystemAccessService
{
    Task<bool> HasAccessAsync(Guid userId, Guid subSystemId, CancellationToken ct = default);
    Task<IReadOnlyList<SubSystem>> GetAccessibleSubSystemsAsync(Guid userId, CancellationToken ct = default);
}
