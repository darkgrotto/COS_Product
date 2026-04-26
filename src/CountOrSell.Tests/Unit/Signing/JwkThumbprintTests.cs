using System.Reflection;
using CountOrSell.Domain.Dtos.Signing;
using Xunit;

namespace CountOrSell.Tests.Unit.Signing;

public class JwkThumbprintTests
{
    private static readonly Type ThumbprintType =
        Assembly.Load("CountOrSell.Api").GetType("CountOrSell.Api.Services.Signing.JwkThumbprint")
        ?? throw new InvalidOperationException("JwkThumbprint type not found");

    private static string Compute(CosJwk jwk) =>
        (string)ThumbprintType
            .GetMethod("Compute", new[] { typeof(CosJwk) })!
            .Invoke(null, new object[] { jwk })!;

    // The thumbprint of the production COS_Backend signing key (kid f9127444b43b4205810139114a0b1a6b)
    // computed via the curl/openssl pipeline in TrustedKeyThumbprints.cs.
    private const string LiveKeyThumbprint = "xlUO-ThKSDDri1Mc0znW_cZx883ZUc4AWRKibJhue6Q";

    [Fact]
    public void Compute_Matches_Live_Key_Thumbprint()
    {
        var jwk = new CosJwk
        {
            Kty = "EC",
            Crv = "P-256",
            Alg = "ES256",
            Use = "sig",
            Kid = "f9127444b43b4205810139114a0b1a6b",
            X = "uimmGe-XB9LHxov4qp0MyU5gXH6Ht3yL7fet3lz6vCE",
            Y = "A9JYdi1mQQcIbbTakOrqCuX1N1LMYnJsSuWGI1kwqIQ"
        };

        Assert.Equal(LiveKeyThumbprint, Compute(jwk));
    }

    [Fact]
    public void Compute_Changes_When_Coordinates_Change()
    {
        var jwk = new CosJwk
        {
            Kty = "EC",
            Crv = "P-256",
            X = "uimmGe-XB9LHxov4qp0MyU5gXH6Ht3yL7fet3lz6vCE",
            Y = "A9JYdi1mQQcIbbTakOrqCuX1N1LMYnJsSuWGI1kwqIQ"
        };

        var original = Compute(jwk);

        // Flip one byte of X (still base64url-shaped) and confirm the thumbprint changes.
        jwk.X = "uimmGe-XB9LHxov4qp0MyU5gXH6Ht3yL7fet3lz6vCF";
        Assert.NotEqual(original, Compute(jwk));
    }

    [Fact]
    public void Compute_Ignores_Non_Required_Members()
    {
        // RFC 7638 thumbprint is computed only over crv, kty, x, y for EC keys.
        // alg/use/kid/created_at must NOT affect the thumbprint.
        var a = new CosJwk
        {
            Kty = "EC",
            Crv = "P-256",
            X = "uimmGe-XB9LHxov4qp0MyU5gXH6Ht3yL7fet3lz6vCE",
            Y = "A9JYdi1mQQcIbbTakOrqCuX1N1LMYnJsSuWGI1kwqIQ",
            Alg = "ES256",
            Use = "sig",
            Kid = "first"
        };
        var b = new CosJwk
        {
            Kty = "EC",
            Crv = "P-256",
            X = "uimmGe-XB9LHxov4qp0MyU5gXH6Ht3yL7fet3lz6vCE",
            Y = "A9JYdi1mQQcIbbTakOrqCuX1N1LMYnJsSuWGI1kwqIQ",
            Alg = "OTHER",
            Use = "different",
            Kid = "second"
        };

        Assert.Equal(Compute(a), Compute(b));
    }
}
