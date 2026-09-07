namespace CountOrSell.Domain.Dtos;

// Outcome of the image half of a content update.
//
// Images are best-effort by design: a failed image is never fatal to the update, because the
// metadata is worth applying even when some artwork is missing. That is only safe if an
// incomplete image set is reported rather than passing silently - an update that stores 99,000
// of 101,000 images and reports plain success leaves the admin with no idea anything is wrong.
public sealed record ImageSyncOutcome(
    int Listed,
    int Saved,
    int SkippedChecksum,
    int RejectedPath,
    int Failed,
    int RateLimited)
{
    public static readonly ImageSyncOutcome None = new(0, 0, 0, 0, 0, 0);

    // Listed in the signed manifest but not in the store when the update finished.
    public int Missing => Listed - Saved;

    public bool IsComplete => Missing <= 0;

    // Short reason for the shortfall, ordered by what an admin should act on first.
    // A request limit is called out separately because it is the one cause that is
    // external, self-inflicted and fixable (raise the plan or slow the sync down),
    // rather than a bad package.
    public string Explain() =>
        RateLimited > 0
            ? $"{RateLimited} refused by the package origin (HTTP 429 - request limit reached)"
            : Failed > 0
                ? $"{Failed} could not be fetched or stored"
                : SkippedChecksum > 0
                    ? $"{SkippedChecksum} failed checksum verification"
                    : RejectedPath > 0
                        ? $"{RejectedPath} had unusable paths in the manifest"
                        : $"{Missing} unaccounted for";
}
