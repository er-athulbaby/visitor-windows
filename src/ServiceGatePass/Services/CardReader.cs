using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ServiceGatePass.Services;

public record CardData(string Cpr, string NameEn, string? NameAr, string? Employer, string? ExpiryDate, string? PhotoBase64);

public record CardReadResult(CardData? Card, string? Error);

/// Talks to the vendor's "CIO GCC CardRead Server" REST service on this PC.
public class CardReader(HttpClient http, string url)
{
    public const string DefaultUrl = "http://localhost:5050/api/operation/ReadCard";

    const string RequestBody = """
        {"ReadCardInfo":false,"ReadPersonalInfo":true,"ReadAddressDetails":false,"ReadBiometrics":false,"ReadEmploymentInfo":true,"ReadImmigrationDetails":false,"ReadTrafficDetails":false,"SilentReading":false,"ReaderIndex":-1,"ReaderName":"","OutputFormat":"JSON","ValidateCard":false}
        """;

    public async Task<CardReadResult> ReadAsync(CancellationToken ct = default)
    {
        string json;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            // The server parses the raw body, so send it as text/plain like the vendor sample does.
            using var response = await http.PostAsync(url, new StringContent(RequestBody, Encoding.UTF8, "text/plain"), timeout.Token);
            if (!response.IsSuccessStatusCode)
                return new(null, string.Format(L.T("Card reader returned HTTP {0}."), (int)response.StatusCode));
            json = await response.Content.ReadAsStringAsync(timeout.Token);
        }
        catch (HttpRequestException) { return NotRunning(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return NotRunning(); }

        return Parse(json);
    }

    static CardReadResult NotRunning() =>
        new(null, L.T("Card reader service is not running — start CIO GCC CardRead Server."));

    public static CardReadResult Parse(string json)
    {
        JsonElement root;
        try { root = JsonDocument.Parse(json).RootElement; }
        catch (JsonException) { return new(null, L.T("Card reader sent an unreadable reply.")); }

        // Errors come back as a normal response with ErrorDescription filled in.
        if (Text(root, "ErrorDescription") is string error) return new(null, error);

        var misc = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("MiscellaneousTextData", out var m) ? m : default;
        var cpr = Text(root, "IdNumber") ?? Text(misc, "CPRNO");
        if (cpr == null) return new(null, L.T("No card detected. Enter CPR manually."));

        var name = Text(root, "EnglishFullName")
            ?? string.Join(" ", new[] { Text(root, "EnglishFirstName"), Text(root, "EnglishLastName") }.Where(s => s != null));
        var employer = Text(root, "EmployerName") ?? Text(root, "EmploymentNameEnglish") ?? Text(root, "SponserNameEnglish");

        return new(new CardData(cpr, name, Text(root, "ArabicFullName"), employer,
            Text(root, "CardexpiryDate"), Text(root, "PhotoB64Encoded")), null);
    }

    static string? Text(JsonElement e, string property) =>
        e.ValueKind == JsonValueKind.Object
        && e.TryGetProperty(property, out var v)
        && v.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(v.GetString())
            ? v.GetString()!.Trim()
            : null;
}
