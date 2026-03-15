namespace CountOrSell.Wizard.Services;

public static class UpdateCheckTimeGenerator
{
    public static string Generate()
    {
        var rng = new Random();
        int hour = rng.Next(0, 24);
        int minute = rng.Next(0, 60);
        return $"{hour:D2}:{minute:D2}";
    }
}
