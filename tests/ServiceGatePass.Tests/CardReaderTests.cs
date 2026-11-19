using System.IO;
using System.Net;
using System.Net.Http;
using ServiceGatePass.Services;

namespace ServiceGatePass.Tests;

public class CardReaderTests
{
    class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Last;
        public string? LastBody;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;
            LastBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
            return respond(request);
        }
    }

    static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json) };

    [Fact]
    public void Parse_reads_full_name_cpr_arabic_and_employer()
    {
        var r = CardReader.Parse("""
            {"IdNumber":"900000001","EnglishFullName":"ALI HASAN","ArabicFullName":"علي حسن",
             "EmployerName":"ACME WLL","CardexpiryDate":"2030-01-31","PhotoB64Encoded":"AAAA","ErrorDescription":""}
            """);

        Assert.Null(r.Error);
        Assert.Equal(new CardData("900000001", "ALI HASAN", "علي حسن", "ACME WLL", "2030-01-31", "AAAA"), r.Card);
    }

    [Fact]
    public void Parse_falls_back_to_misc_cpr_and_first_last_name()
    {
        var r = CardReader.Parse("""
            {"IdNumber":"","MiscellaneousTextData":{"CPRNO":"900000002"},"EnglishFirstName":"ALI","EnglishLastName":"HASAN"}
            """);

        Assert.Equal("900000002", r.Card!.Cpr);
        Assert.Equal("ALI HASAN", r.Card.NameEn);
    }

    [Theory]
    [InlineData("""{"IdNumber":"900000001","EmployerName":"E","EmploymentNameEnglish":"M","SponserNameEnglish":"S"}""", "E")]
    [InlineData("""{"IdNumber":"900000001","EmploymentNameEnglish":"M","SponserNameEnglish":"S"}""", "M")]
    [InlineData("""{"IdNumber":"900000001","SponserNameEnglish":"S"}""", "S")]
    [InlineData("""{"IdNumber":"900000001"}""", null)]
    public void Parse_employer_fallback_order(string json, string? expected) =>
        Assert.Equal(expected, CardReader.Parse(json).Card!.Employer);

    [Fact]
    public void Parse_error_description_is_an_error_even_on_success_response()
    {
        var r = CardReader.Parse("""{"IdNumber":"","ErrorDescription":"The smart card has been removed, so further communication is not possible."}""");
        Assert.Null(r.Card);
        Assert.Equal("The smart card has been removed, so further communication is not possible.", r.Error);
    }

    [Fact]
    public void Parse_without_cpr_means_no_card() =>
        Assert.Equal("No card detected. Enter CPR manually.", CardReader.Parse("""{"EnglishFullName":"X"}""").Error);

    [Fact]
    public void Parse_invalid_json_is_an_error() =>
        Assert.Equal("Card reader sent an unreadable reply.", CardReader.Parse("<html>").Error);

    [Fact]
    public async Task ReadAsync_posts_the_request_as_text_plain()
    {
        var stub = new StubHandler(_ => Json("""{"IdNumber":"900000001","EnglishFullName":"A"}"""));
        var reader = new CardReader(new HttpClient(stub), "http://localhost:5050/api/operation/ReadCard");

        var r = await reader.ReadAsync();

        Assert.Equal("900000001", r.Card!.Cpr);
        Assert.Equal(HttpMethod.Post, stub.Last!.Method);
        Assert.Equal("text/plain", stub.Last.Content!.Headers.ContentType!.MediaType);
        Assert.Contains("\"ReadPersonalInfo\":true", stub.LastBody);
        Assert.Contains("\"ReadEmploymentInfo\":true", stub.LastBody);
        Assert.Contains("\"ReadBiometrics\":false", stub.LastBody);
    }

    [Fact]
    public async Task ReadAsync_connection_refused_says_service_not_running()
    {
        var stub = new StubHandler(_ => throw new HttpRequestException("refused"));
        var r = await new CardReader(new HttpClient(stub), CardReader.DefaultUrl).ReadAsync();
        Assert.Equal("Card reader service is not running — start CIO GCC CardRead Server.", r.Error);
    }

    [Fact]
    public async Task ReadAsync_http_error_is_reported()
    {
        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var r = await new CardReader(new HttpClient(stub), CardReader.DefaultUrl).ReadAsync();
        Assert.Equal("Card reader returned HTTP 500.", r.Error);
    }
}
