using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ServiceGatePass.Tests;

public class TranslationCoverageTests
{
    static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ServiceGatePass.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("ServiceGatePass.sln not found above " + AppContext.BaseDirectory);
    }

    static bool IsSource(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar);
        return !parts.Contains("obj") && !parts.Contains("bin") && !parts.Contains("Migrations");
    }

    [Fact]
    public void Every_UI_string_has_an_Arabic_translation()
    {
        var src = Path.Combine(RepoRoot(), "src", "ServiceGatePass");
        var keys = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories).Where(IsSource))
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"L\.T\(""((?:[^""\\]|\\.)*)""\)"))
                keys.Add(Regex.Unescape(m.Groups[1].Value));

        foreach (var file in Directory.EnumerateFiles(src, "*.xaml", SearchOption.AllDirectories).Where(IsSource))
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"\{l:T '([^']*)'\}"))
                keys.Add(m.Groups[1].Value);

        var ar = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(src, "Resources", "ar.json")))!;
        var missing = keys.Where(k => !ar.ContainsKey(k)).ToList();

        Assert.True(missing.Count == 0, "Missing Arabic for:\n" + string.Join("\n", missing));
    }
}
