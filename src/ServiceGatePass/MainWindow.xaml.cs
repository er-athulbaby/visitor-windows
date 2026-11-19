using System.Windows;
using System.Windows.Controls;
using ServiceGatePass.Data;
using ServiceGatePass.Services;
using ServiceGatePass.Views;

namespace ServiceGatePass;

public partial class MainWindow : Window
{
    bool replacing;

    public MainWindow(int selectedTab = 0)
    {
        InitializeComponent();
        FlowDirection = L.IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        UserName.Text = App.CurrentUser.DisplayName;
        UserRole.Text = App.CurrentUser.RoleText;
        UserInitials.Text = InsideRow.InitialsOf(App.CurrentUser.DisplayName);
        LanguageButton.Content = L.IsArabic ? "English" : "العربية";
        RefreshHeader();

        Tabs.Items.Add(new TabItem { Header = L.T("Gate"), Content = new GateView() });
        if (App.CurrentUser.Role == Role.Admin)
        {
            Tabs.Items.Add(new TabItem { Header = L.T("History"), Content = new HistoryView() });
            Tabs.Items.Add(new TabItem { Header = L.T("Dashboard"), Content = new DashboardView() });
            Tabs.Items.Add(new TabItem { Header = L.T("Settings"), Content = new SettingsView() });
            Tabs.Items.Add(new TabItem { Header = L.T("Users"), Content = new UsersView() });
        }
        string[] icons = ["IconGate", "IconHistory", "IconDashboard", "IconSettings", "IconUsers"];
        for (var i = 0; i < Tabs.Items.Count; i++) ((TabItem)Tabs.Items[i]).Tag = FindResource(icons[i]);
        Tabs.SelectedIndex = Math.Min(selectedTab, Tabs.Items.Count - 1);

        App.BackupStatusChanged += ShowBackupStatus;
        ShowBackupStatus();
        Closed += (_, _) =>
        {
            App.BackupStatusChanged -= ShowBackupStatus;
            if (!replacing) Application.Current.Shutdown();
        };
    }

    public void RefreshHeader()
    {
        using var db = App.OpenDb();
        var settings = new SettingsStore(db);
        HotelName.Text = settings.Get(SettingsStore.HotelName) ?? "";
        Logo.Source = App.LoadImage(settings.Get(SettingsStore.LogoPath));
        LogoTile.Visibility = Logo.Source == null ? Visibility.Collapsed : Visibility.Visible;
        BrandMark.Visibility = Logo.Source == null ? Visibility.Visible : Visibility.Collapsed;
        Title = $"Service Gate Pass — {HotelName.Text}";
    }

    void ShowBackupStatus()
    {
        var message = App.BackupError
            ?? (App.BackupFolderMissing && App.CurrentUser.Role == Role.Admin
                ? L.T("No backup folder is set. Choose one in Settings.")
                : null);
        Banner.Text = message ?? "";
        BannerBar.Visibility = message == null ? Visibility.Collapsed : Visibility.Visible;
    }

    void Language_Click(object sender, RoutedEventArgs e)
    {
        var lang = L.IsArabic ? "en" : "ar";
        using (var db = App.OpenDb()) new SettingsStore(db).Set(SettingsStore.Language, lang);
        L.Use(lang);
        var next = new MainWindow(Tabs.SelectedIndex);
        replacing = true;
        Application.Current.MainWindow = next;
        next.Show();
        Close();
    }

    void Logout_Click(object sender, RoutedEventArgs e)
    {
        replacing = true;
        Close();
        ((App)Application.Current).ShowLogin();
    }
}
