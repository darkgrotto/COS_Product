namespace CountOrSell.Domain.Services;

public interface IPackageVerifier
{
    // Verifies a single file's checksum against the per-package manifest checksums dict.
    // expectedChecksum format: "sha256:<hex_lowercase>"
    bool VerifyFileChecksum(byte[] fileBytes, string expectedChecksum);
}
