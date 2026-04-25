namespace CountOrSell.Data.Repositories;

public readonly record struct CardSummary(
    string Name,
    decimal? MarketValue,
    string SetCode,
    string? OracleRulingUrl);
