using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using OneAccess.Infrastructure.Identity;

namespace OneAccess.API.Endpoints;

/// <summary>
/// Exposes public keys for sub-systems to validate RS256 JWT tokens locally (OneAccess.md Section 10 & 14).
/// </summary>
public static class JwksEndpoints
{
    public static IEndpointRouteBuilder MapJwksEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/jwks.json", (RsaKeyProvider rsaKeyProvider) =>
        {
            var keys = rsaKeyProvider.GetAllValidationKeys();

            var jwks = keys.Select(k =>
            {
                var rsa = k.Rsa ?? RSA.Create();
                if (k.Parameters.Modulus != null)
                {
                    rsa.ImportParameters(k.Parameters);
                }

                var parameters = rsa.ExportParameters(false);

                return new
                {
                    kty = "RSA",
                    use = "sig",
                    alg = "RS256",
                    kid = k.KeyId,
                    n = Base64UrlEncoder.Encode(parameters.Modulus),
                    e = Base64UrlEncoder.Encode(parameters.Exponent)
                };
            }).ToList();

            return Results.Ok(new { keys = jwks });
        })
        .AllowAnonymous()
        .WithName("GetJwks")
        .WithTags("Auth");

        return app;
    }
}
