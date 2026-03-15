namespace CountOrSell.Api.Services;

public record UserServiceResult(bool Success, string? Error = null)
{
    public static UserServiceResult Ok() => new(true);
    public static UserServiceResult Fail(string error) => new(false, error);
}
