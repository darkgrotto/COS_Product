namespace CountOrSell.Domain.Models;

/// <summary>
/// Condition grade for a card or sealed product.
/// Stored as enum - not a reference table.
/// </summary>
public enum CardCondition
{
    NM,
    LP,
    MP,
    HP,
    DMG
}
