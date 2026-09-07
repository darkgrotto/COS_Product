namespace CountOrSell.Domain.Services;

// Whether a card may be recorded with a given treatment.
//
// A card's treatments come from the update package: the Backend associates each printing
// with the union of its finish- and promo-type-derived treatments, and Product stores that
// as a comma-joined list. A foil-only serialized printing carries {foil, serialized} and no
// regular, so pairing it with Regular records something that does not exist.
//
// The pairing is only enforced when the package actually said what the card has. A card we
// hold no record for, or one whose canonical data predates per-card treatments, is allowed
// through: blocking those would reject legitimate pre-existing entries and legacy imports
// for no gain, since the absence of data is not evidence the pairing is wrong.
public static class CardTreatmentRule
{
    // validTreatments is the stored comma-joined list (Card.ValidTreatments), which is null
    // or empty when the package has not told us.
    public static bool Accepts(string? validTreatments, string? treatmentKey)
    {
        if (string.IsNullOrWhiteSpace(validTreatments)) return true;
        if (string.IsNullOrWhiteSpace(treatmentKey)) return false;

        foreach (var candidate in validTreatments.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(candidate.Trim(), treatmentKey.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // The treatments a card does offer, for error messages. Empty when unconstrained.
    public static string[] Offered(string? validTreatments) =>
        string.IsNullOrWhiteSpace(validTreatments)
            ? []
            : validTreatments.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToArray();
}
