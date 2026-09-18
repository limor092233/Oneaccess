using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Domain.Entities;
using OneAccess.Infrastructure.Options;

namespace OneAccess.Infrastructure.Services;

/// <summary>
/// Service managing first-run setup detection and cryptographic code generation/validation.
/// </summary>
public class SetupCodeService : ISetupCodeService
{
    private readonly IReadDbContext _readDbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SetupOptions _setupOptions;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<SetupCodeService> _logger;

    private static readonly char[] Characters = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();

    public SetupCodeService(
        IReadDbContext readDbContext,
        IUnitOfWork unitOfWork,
        IOptions<SetupOptions> setupOptions,
        IDateTimeProvider dateTimeProvider,
        ILogger<SetupCodeService> logger)
    {
        _readDbContext = readDbContext;
        _unitOfWork = unitOfWork;
        _setupOptions = setupOptions.Value;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<bool> IsSetupRequiredAsync(CancellationToken ct = default)
    {
        return !await _readDbContext.Users.AnyAsync(u => u.IsSystemAdministrator, ct);
    }

    public async Task<string> GenerateAndStoreCodeAsync(CancellationToken ct = default)
    {
        var rawCode = GenerateRandomCode(_setupOptions.CodeLength);
        var hashCode = HashCode(rawCode);
        var now = _dateTimeProvider.UtcNow;
        var expiresAt = now.AddMinutes(_setupOptions.CodeExpiryMinutes);

        var setupCode = new SetupCode
        {
            CodeHash = hashCode,
            ExpiresAt = expiresAt
        };

        await _unitOfWork.Repository<SetupCode>().AddAsync(setupCode, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        if (_setupOptions.ConsoleOutputEnabled)
        {
            // Emitted with IsSetupCode structured property per OneAccess.md Section 12
            using (_logger.BeginScope(new Dictionary<string, object> { ["IsSetupCode"] = true }))
            {
                _logger.LogInformation("\n=========================================\n[SETUP] First-Run System Administrator Setup\n[SETUP] Setup Code: {SetupCode}\n[SETUP] Expires in {ExpiryMinutes} minutes\n=========================================\n",
                    rawCode, _setupOptions.CodeExpiryMinutes);
            }
        }

        return rawCode;
    }

    public async Task<bool> ValidateAndConsumeCodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        var cleanCode = code.Trim().ToUpperInvariant().Replace("-", "");
        var hashCode = HashCode(cleanCode);
        var now = _dateTimeProvider.UtcNow;

        var record = await _readDbContext.SetupCodes
            .FirstOrDefaultAsync(sc => sc.CodeHash == hashCode, ct);

        if (record == null || record.ConsumedAt != null || record.ExpiresAt < now)
        {
            return false;
        }

        record.ConsumedAt = now;
        _unitOfWork.Repository<SetupCode>().Update(record);
        await _unitOfWork.SaveChangesAsync(ct);

        return true;
    }

    private static string GenerateRandomCode(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            result.Append(Characters[bytes[i] % Characters.Length]);
        }

        var str = result.ToString();
        if (str.Length == 8)
        {
            return $"{str.Substring(0, 4)}-{str.Substring(4, 4)}";
        }

        return str;
    }

    private static string HashCode(string code)
    {
        var normalized = code.Trim().ToUpperInvariant().Replace("-", "");
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
