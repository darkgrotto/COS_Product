namespace CountOrSell.Domain.Dtos.Packages;

public class SetDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalCards { get; set; }
    public DateOnly? ReleaseDate { get; set; }
}
