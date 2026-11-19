using System.IO;
using Microsoft.Data.Sqlite;
using ServiceGatePass.Services;

namespace ServiceGatePass.Tests;

public sealed class BackupServiceTests : IDisposable
{
    readonly string dir = Path.Combine(Path.GetTempPath(), "sgp-backup-" + Guid.NewGuid().ToString("N"));
    readonly string dbPath;

    public BackupServiceTests()
    {
        Directory.CreateDirectory(dir);
        dbPath = Path.Combine(dir, "gate.db");
        Exec(dbPath, "CREATE TABLE t(x INTEGER); INSERT INTO t VALUES (42);");
    }

    static long Exec(string path, string sql)
    {
        using var conn = new SqliteConnection($"Data Source={path};Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void Backup_copies_the_data_with_a_dated_name()
    {
        var folder = Path.Combine(dir, "backups");
        var path = BackupService.BackupNow(dbPath, folder, new DateTime(2026, 11, 19));

        Assert.Equal(Path.Combine(folder, "gate-2026-11-19.db"), path);
        Assert.Equal(42, Exec(path, "SELECT x FROM t"));
    }

    [Fact]
    public void Same_day_backup_overwrites()
    {
        var folder = Path.Combine(dir, "backups");
        BackupService.BackupNow(dbPath, folder, new DateTime(2026, 11, 19));
        Exec(dbPath, "UPDATE t SET x = 7");
        var path = BackupService.BackupNow(dbPath, folder, new DateTime(2026, 11, 19));

        Assert.Equal(7, Exec(path, "SELECT x FROM t"));
        Assert.Single(Directory.GetFiles(folder));
    }

    [Fact]
    public void Keeps_only_the_newest_30()
    {
        var folder = Path.Combine(dir, "backups");
        Directory.CreateDirectory(folder);
        for (var i = 1; i <= 31; i++)
            File.WriteAllText(Path.Combine(folder, $"gate-2026-10-{i:00}.db"), "");
        File.WriteAllText(Path.Combine(folder, "notes.txt"), "keep me");

        BackupService.BackupNow(dbPath, folder, new DateTime(2026, 11, 19));

        var backups = Directory.GetFiles(folder, "gate-*.db").Select(Path.GetFileName).Order().ToList();
        Assert.Equal(30, backups.Count);
        Assert.Equal("gate-2026-10-03.db", backups[0]);
        Assert.Equal("gate-2026-11-19.db", backups[^1]);
        Assert.True(File.Exists(Path.Combine(folder, "notes.txt")));
    }
}
