using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OneAccess.Infrastructure.Options;

namespace OneAccess.Infrastructure.Identity;

/// <summary>
/// Provides RSA signing and validation keys for RS256 JWT tokens.
/// </summary>
public class RsaKeyProvider
{
    private readonly JwtOptions _options;
    private readonly ILogger<RsaKeyProvider> _logger;
    private readonly IHostEnvironment? _environment;
    private readonly Dictionary<string, RSA> _keys = new();
    private string _activeKid = string.Empty;

    public RsaKeyProvider(IOptions<JwtOptions> options, ILogger<RsaKeyProvider> logger, IHostEnvironment? environment = null)
    {
        _options = options.Value;
        _logger = logger;
        _environment = environment;
        InitializeKeys();
    }

    private void InitializeKeys()
    {
        if (!string.IsNullOrWhiteSpace(_options.SigningKeysDirectory) && Directory.Exists(_options.SigningKeysDirectory))
        {
            var keyFiles = Directory.GetFiles(_options.SigningKeysDirectory, "*.pem");
            foreach (var keyFile in keyFiles)
            {
                try
                {
                    var kid = Path.GetFileNameWithoutExtension(keyFile);
                    var pemText = File.ReadAllText(keyFile);
                    var rsa = RSA.Create();
                    rsa.ImportFromPem(pemText);
                    _keys[kid] = rsa;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load RSA key file {File}", keyFile);
                }
            }
        }

        if (_keys.Count == 0)
        {
            if (_environment != null && !_environment.IsDevelopment() && !_environment.IsEnvironment("Test"))
            {
                throw new InvalidOperationException(
                    "JWT signing keys directory 'Jwt:SigningKeysDirectory' is not configured or contains no valid .pem keys. " +
                    "In non-development environments, a valid directory containing RSA .pem private keys is required.");
            }

            // Dev/Fallback in-memory 2048-bit RSA key
            var devKid = string.IsNullOrWhiteSpace(_options.ActiveKeyId) ? "dev-key-1" : _options.ActiveKeyId;
            var rsa = RSA.Create(2048);
            _keys[devKid] = rsa;
            _activeKid = devKid;
            _logger.LogInformation("Generated in-memory RSA key with KeyId: {KeyId}", devKid);
        }
        else
        {
            _activeKid = !string.IsNullOrWhiteSpace(_options.ActiveKeyId) && _keys.ContainsKey(_options.ActiveKeyId)
                ? _options.ActiveKeyId
                : _keys.Keys.First();
        }
    }

    public (string KeyId, RsaSecurityKey SigningKey) GetActiveSigningKey()
    {
        var rsa = _keys[_activeKid];
        var key = new RsaSecurityKey(rsa) { KeyId = _activeKid };
        return (_activeKid, key);
    }

    public IReadOnlyList<RsaSecurityKey> GetAllValidationKeys()
    {
        return _keys.Select(kvp => new RsaSecurityKey(kvp.Value) { KeyId = kvp.Key }).ToList();
    }
}
