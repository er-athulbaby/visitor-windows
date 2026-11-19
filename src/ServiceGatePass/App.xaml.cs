using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using ServiceGatePass.Data;
using ServiceGatePass.Services;
using ServiceGatePass.Views;

namespace ServiceGatePass;

public partial class App : Application
{
    public static readonly string DataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ServiceGatePass");
    public static string DbPath => Path.Combine(DataDir, "gate.db");
    public static readonly HttpClient Http = new();
    public static User CurrentUser { get; set; } = null!;

    public static string? BackupError { get; private set; }
    public static bool BackupFolderMissing { get; private set; }
    public static event Action? BackupStatusChanged;

    readonly DispatcherTimer backupTimer = new() { Interval = TimeSpan.FromHours(24) };

    public static GateDb OpenDb() => new(GateDb.OptionsFor(DbPath));

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, a) =>
        {
            Log.Error(a.Exception);
            MessageBox.Show(L.T("Something went wrong. Details were saved to the log.") + "\n\n" + a.Exception.Message,
                "Service Gate Pass", MessageBoxButton.OK, MessageBoxImage.Error);
            a.Handled = true;
            // OnExplicitShutdown: if the error left no window open, exit rather than run invisibly.
            if (!Windows.OfType<Window>().Any(w => w.IsVisible)) Shutdown(1);
        };

        try
        {
            Directory.CreateDirectory(DataDir);
            var existed = File.Exists(DbPath);
            bool needsSetup;
            using (var db = OpenDb())
            {
                if (existed && db.Database.GetPendingMigrations().Any())
                    BackupService.Copy(DbPath, Path.Combine(DataDir, $"gate-before-upgrade-{DateTime.Now:yyyyMMdd-HHmmss}.db"));
                db.Database.Migrate();
                L.Use(new SettingsStore(db).Get(SettingsStore.Language) ?? "en");
                needsSetup = !new UserService(db).AnyUsers();
            }

            if (needsSetup && new SetupWindow().ShowDialog() != true) { Shutdown(); return; }

            RunBackup();
            backupTimer.Tick += (_, _) => RunBackup();
            backupTimer.Start();
            ShowLogin();
        }
        catch (Exception ex)
        {
            // With OnExplicitShutdown a failed start would leave an invisible process; exit instead.
            Log.Error(ex);
            MessageBox.Show(ex.Message, "Service Gate Pass", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    public void ShowLogin()
    {
        var login = new LoginWindow();
        if (login.ShowDialog() != true) { Shutdown(); return; }
        CurrentUser = login.User!;
        var main = new MainWindow();
        MainWindow = main;
        main.Show();
    }

    public static void RunBackup()
    {
        try
        {
            using var db = OpenDb();
            var folder = new SettingsStore(db).Get(SettingsStore.BackupFolder);
            BackupFolderMissing = string.IsNullOrWhiteSpace(folder);
            BackupError = null;
            if (!BackupFolderMissing) BackupService.BackupNow(DbPath, folder!, DateTime.Today);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            BackupError = string.Format(L.T("Backup failed: {0}"), ex.Message);
        }
        BackupStatusChanged?.Invoke();
    }

    public static BitmapImage? LoadImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;              // don't lock the file
        img.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // pick up a replaced logo
        img.UriSource = new Uri(path);
        img.EndInit();
        img.Freeze();
        return img;
    }

    public static BitmapImage? ImageFromBase64(string? b64)
    {
        if (string.IsNullOrWhiteSpace(b64)) return null;
        var comma = b64.IndexOf(',');
        if (comma >= 0) b64 = b64[(comma + 1)..]; // tolerate "data:image/...;base64,"
        try
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = new MemoryStream(Convert.FromBase64String(b64));
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException) { return null; }
    }

    public static string CopyLogo(string source)
    {
        var dest = Path.Combine(DataDir, "logo" + Path.GetExtension(source).ToLowerInvariant());
        if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
            File.Copy(source, dest, overwrite: true);
        return dest;
    }
}
