namespace CountOrSell.Domain.Services;

public interface IPackageVerifier
{
    bool VerifyChecksum(Stream packageStream, string expectedSha256);
}
