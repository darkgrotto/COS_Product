namespace CountOrSell.Domain.Dtos.Packages;

public class TreatmentDto
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
