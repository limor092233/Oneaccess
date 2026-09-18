namespace OneAccess.Application.Common.Interfaces;

/// <summary>
/// Service contract for managing first-run System Administrator setup codes.
/// </summary>
public interface ISetupCodeService
{
    Task<bool> IsSetupRequiredAsync(CancellationToken ct = default);
    Task<string> GenerateAndStoreCodeAsync(CancellationToken ct = default);
    Task<bool> ValidateAndConsumeCodeAsync(string code, CancellationToken ct = default);
}
