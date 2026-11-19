using System.Text.Json;
using System.Windows.Markup;

namespace ServiceGatePass;

/// English text is the key; Arabic comes from the embedded ar.json.
public static class L
{
    static Dictionary<string, string>? arabic;

    public static bool IsArabic { get; private set; }

    public static void Use(string lang)
    {
        IsArabic = lang == "ar";
        if (IsArabic && arabic == null)
        {
            using var stream = typeof(L).Assembly.GetManifestResourceStream("ar.json")
                ?? throw new InvalidOperationException("ar.json is not embedded.");
            arabic = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
        }
    }

    public static string T(string english) =>
        IsArabic && arabic!.TryGetValue(english, out var text) ? text : english;
}

/// XAML: {l:T 'Check in'}
[MarkupExtensionReturnType(typeof(string))]
public class TExtension : MarkupExtension
{
    public TExtension(string text) => Text = text;

    [ConstructorArgument("text")]
    public string Text { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) => L.T(Text);
}
