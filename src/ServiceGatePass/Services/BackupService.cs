using System.IO;
using Microsoft.Data.Sqlite;

namespace ServiceGatePass.Services;

public static class BackupService
{
    /// SQLite online backup: consistent even while the app has the database open.
    public static void Copy(string dbPath, string destPath)
    {
        using var source = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        using var dest = new SqliteConnection($"Data Source={destPath};Pooling=False");
        source.Open();
        dest.Open();
        source.BackupDatabase(dest);
    }

    public static string BackupNow(string dbPath, string folder, DateTime today, int keep = 30)
    {
        Directory.CreateDirectory(folder);
        var dest = Path.Combine(folder, $"gate-{today:yyyy-MM-dd}.db");
        Copy(dbPath, dest);

        foreach (var old in Directory.GetFiles(folder, "gate-????-??-??.db").OrderDescending().Skip(keep))
            File.Delete(old);
        return dest;
    }
}
