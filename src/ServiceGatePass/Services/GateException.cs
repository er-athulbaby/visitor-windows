namespace ServiceGatePass.Services;

/// A rule the user broke (already inside, badge in use...). Message is already translated.
public class GateException(string message) : Exception(message);

public static class Str
{
    public static string? OrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
