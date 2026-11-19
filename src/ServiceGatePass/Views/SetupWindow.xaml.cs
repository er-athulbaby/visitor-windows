using System.Windows;
using Microsoft.Win32;
using ServiceGatePass.Data;
using ServiceGatePass.Services;

namespace ServiceGatePass.Views;

public partial class SetupWindow : Window
{
    string? logoSource;

    public SetupWindow()
    {
        InitializeComponent();
        FlowDirection = L.IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Loaded += (_, _) => HotelNameBox.Focus();
    }

    void ChooseLogo_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dlg.ShowDialog(this) != true) return;
        logoSource = dlg.FileName;
        LogoPreview.Source = App.LoadImage(logoSource);
    }

    void Finish_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        var locations = LocationsBox.Text.Split('\n').Select(s => s.Trim()).Where(s => s != "").Distinct().ToList();
        if (string.IsNullOrWhiteSpace(HotelNameBox.Text)) { ErrorText.Text = L.T("Hotel name is required."); return; }
        if (locations.Count == 0) { ErrorText.Text = L.T("Add at least one location."); return; }
        if (PasswordInput.Password != ConfirmInput.Password) { ErrorText.Text = L.T("Passwords do not match."); return; }

        try
        {
            using var db = App.OpenDb();
            using var tx = db.Database.BeginTransaction();
            new UserService(db).Create(UsernameBox.Text, DisplayNameBox.Text, PasswordInput.Password, Role.Admin);
            var settings = new SettingsStore(db);
            settings.Set(SettingsStore.HotelName, HotelNameBox.Text.Trim());
            if (logoSource != null) settings.Set(SettingsStore.LogoPath, App.CopyLogo(logoSource));
            for (var i = 0; i < locations.Count; i++)
                db.Locations.Add(new Location { Name = locations[i], SortOrder = i });
            db.SaveChanges();
            tx.Commit();
            DialogResult = true;
        }
        catch (GateException ex) { ErrorText.Text = ex.Message; }
    }
}
