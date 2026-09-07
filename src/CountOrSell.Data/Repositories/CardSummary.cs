namespace CountOrSell.Data.Repositories;

public readonly record struct CardSummary(
    string Name,
    decimal? MarketValue,
    string SetCode,
    string? OracleRulingUrl,
    // True when a full package no longer lists this card. The user still holds it, so the
    // record is kept and still resolves - this is what lets a view say so rather than
    // silently showing a holding whose card has quietly vanished from the catalog.
    bool RetiredFromCatalog = false);
