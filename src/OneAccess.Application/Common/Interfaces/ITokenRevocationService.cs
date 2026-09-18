namespace OneAccess.Application.Common.Interfaces;

/// <summary>
/// Service contract managing token revocation via Redis timestamps.
/// </summary>
public interface ITokenRevocationService
{
    Task RevokeAsync(Guid userId, DateTimeOffset revokedAfter, CancellationToken ct = default);
    Task<bool> IsRevokedAsync(Guid userId, long tokenIssuedAtUnixSeconds, CancellationToken ct = default);
}
