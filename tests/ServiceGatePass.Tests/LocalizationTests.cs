using ServiceGatePass.Data;

namespace ServiceGatePass.Tests;

public class LocalizationTests
{
    [Fact]
    public void English_is_returned_unchanged()
    {
        L.Use("en");
        Assert.Equal("Check in", L.T("Check in"));
    }

    [Fact]
    public void Arabic_is_used_when_selected_and_falls_back_to_english()
    {
        try
        {
            L.Use("ar");
            Assert.True(L.IsArabic);
            Assert.Equal("تسجيل الدخول", L.T("Check in"));
            Assert.Equal("No such key", L.T("No such key"));
        }
        finally { L.Use("en"); }
    }

    [Fact]
    public void Location_display_uses_arabic_name_only_in_arabic()
    {
        var loc = new Location { Name = "Kitchen", NameAr = "المطبخ" };
        try
        {
            L.Use("ar");
            Assert.Equal("المطبخ", loc.Display);
            L.Use("en");
            Assert.Equal("Kitchen", loc.Display);
        }
        finally { L.Use("en"); }
    }
}
