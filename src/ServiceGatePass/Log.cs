using System.IO;

namespace ServiceGatePass;

public static class Log
{
    public static void Error(Exception ex)
    {
        try
        {
            var dir = Path.Combine(App.DataDir, "logs");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd}.log"),
                $"{DateTime.Now:HH:mm:ss} {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
