using System.Security.Cryptography;
using CountOrSell.Api.Services.Signing;
using CountOrSell.Domain.Dtos.Signing;
using CountOrSell.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CountOrSell.Tests.Unit.Signing;

public class ManifestSignatureVerifierTests
{
    private static (ECDsa ecdsa, CosJwk jwk) NewTestKey(string kid)
    {
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var p = ecdsa.ExportParameters(includePrivateParameters: false);
        var jwk = new CosJwk
        {
            Kty = "EC",
            Crv = "P-256",
            Alg = "ES256",
            Use = "sig",
            Kid = kid,
            X = ToBase64Url(p.Q.X!),
            Y = ToBase64Url(p.Q.Y!)
        };
        return (ecdsa, jwk);
    }

    private static string ToBase64Url(byte[] data) =>
        Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static SignedManifestEnvelope SignBytes(ECDsa ecdsa, byte[] bytes, string kid)
    {
        var sig = ecdsa.SignData(
            bytes,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return new SignedManifestEnvelope
        {
            Alg = "ES256",
            Kid = kid,
            Sig = Convert.ToBase64String(sig)
        };
    }

    private static ManifestSignatureVerifier BuildVerifier(CosJwk? returnedKey, string? lookupKid = null)
    {
        var jwks = new Mock<IJwksProvider>();
        jwks.Setup(j => j.GetKeyByKidAsync(
                It.Is<string>(k => lookupKid == null || k == lookupKid),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedKey);
        return new ManifestSignatureVerifier(jwks.Object, NullLogger<ManifestSignatureVerifier>.Instance);
    }

    [Fact]
    public async Task VerifyAsync_Returns_Valid_For_Legitimate_Signature()
    {
        var (ecdsa, jwk) = NewTestKey("kid-1");
        var manifest = "{\"package_type\":\"full\"}"u8.ToArray();
        var envelope = SignBytes(ecdsa, manifest, "kid-1");

        var result = await BuildVerifier(jwk).VerifyAsync(manifest, envelope, default);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task VerifyAsync_Refuses_Tampered_Manifest_Bytes()
    {
        var (ecdsa, jwk) = NewTestKey("kid-1");
        var manifest = "{\"package_type\":\"full\"}"u8.ToArray();
        var envelope = SignBytes(ecdsa, manifest, "kid-1");
        manifest[0] ^= 0x01; // flip one bit

        var result = await BuildVerifier(jwk).VerifyAsync(manifest, envelope, default);

        Assert.Equal(SignatureVerificationStatus.SignatureMismatch, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_Refuses_Tampered_Signature_Bytes()
    {
        var (ecdsa, jwk) = NewTestKey("kid-1");
        var manifest = "{\"package_type\":\"full\"}"u8.ToArray();
        var envelope = SignBytes(ecdsa, manifest, "kid-1");

        // Flip one byte of the (decoded) signature, re-encode.
        var sigBytes = Convert.FromBase64String(envelope.Sig);
        sigBytes[0] ^= 0x01;
        envelope.Sig = Convert.ToBase64String(sigBytes);

        var result = await BuildVerifier(jwk).VerifyAsync(manifest, envelope, default);

        Assert.Equal(SignatureVerificationStatus.SignatureMismatch, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_Refuses_Unknown_Kid()
    {
        var (ecdsa, _) = NewTestKey("kid-1");
        var manifest = "{\"package_type\":\"full\"}"u8.ToArray();
        var envelope = SignBytes(ecdsa, manifest, "rotated-out");

        // JWKS provider returns null for the unknown kid.
        var result = await BuildVerifier(returnedKey: null).VerifyAsync(manifest, envelope, default);

        Assert.Equal(SignatureVerificationStatus.KidNotFound, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_Refuses_Unsupported_Algorithm()
    {
        var (ecdsa, jwk) = NewTestKey("kid-1");
        var manifest = "{\"package_type\":\"full\"}"u8.ToArray();
        var envelope = SignBytes(ecdsa, manifest, "kid-1");
        envelope.Alg = "RS256";

        var result = await BuildVerifier(jwk).VerifyAsync(manifest, envelope, default);

        Assert.Equal(SignatureVerificationStatus.UnsupportedAlgorithm, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_Refuses_Wrong_Length_Signature()
    {
        var (_, jwk) = NewTestKey("kid-1");
        var envelope = new SignedManifestEnvelope
        {
            Alg = "ES256",
            Kid = "kid-1",
            // 70-byte buffer -> not 64 bytes raw P1363 -> rejected as format-invalid.
            Sig = Convert.ToBase64String(new byte[70])
        };

        var result = await BuildVerifier(jwk).VerifyAsync(new byte[1], envelope, default);

        Assert.Equal(SignatureVerificationStatus.SignatureFormatInvalid, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_Refuses_Non_EC_Key()
    {
        var jwk = new CosJwk
        {
            Kty = "RSA",
            Crv = "P-256",
            Kid = "kid-1",
            X = "AA",
            Y = "AA"
        };
        var envelope = new SignedManifestEnvelope
        {
            Alg = "ES256",
            Kid = "kid-1",
            Sig = Convert.ToBase64String(new byte[64])
        };

        var result = await BuildVerifier(jwk).VerifyAsync(new byte[1], envelope, default);

        Assert.Equal(SignatureVerificationStatus.KeyMaterialInvalid, result.Status);
    }
}
