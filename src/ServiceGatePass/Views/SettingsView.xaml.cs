using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using ServiceGatePass.Data;
using ServiceGatePass.Services;

namespace ServiceGatePass.Views;

public partial class SettingsView : UserControl
{
    string? newLogo;

    public SettingsView()
    {
        InitializeComponent();
        using var db = App.OpenDb();
        var s = new SettingsStore(db);
        HotelNameBox.Text = s.Get(SettingsStore.HotelName) ?? "";
        LogoImage.Source = App.LoadImage(s.Get(SettingsStore.LogoPath));
        BadgeBox.IsChecked = s.GetBool(SettingsStore.BadgeEnabled);
        SlotBox.IsChecked = s.GetBool(SettingsStore.SlotEnabled);
        SlotCountBox.Text = s.GetInt(SettingsStore.SlotCount, 50).ToString();
        BackupBox.Text = s.Get(SettingsStore.BackupFolder) ?? "";
        ReaderBox.Text = s.Get(SettingsStore.ReaderUrl) ?? CardReader.DefaultUrl;
        LoadLocations(db);
    }

    void LoadLocations(GateDb db)
    {
        var selectedId = (LocationsList.SelectedItem as Location)?.Id;
        var list = db.Locations.OrderBy(l => l.SortOrder).ToList();
        LocationsList.ItemsSource = list;
        LocationsList.SelectedItem = list.FirstOrDefault(l => l.Id == selectedId);
    }

    void Msg(string text, bool error)
    {
        MessageText.Text = text;
        MessageText.Foreground = error ? Brushes.DarkRed : Brushes.DarkGreen;
    }

    void ChooseLogo_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dlg.ShowDialog() != true) return;
        newLogo = dlg.FileName;
        LogoImage.Source = App.LoadImage(newLogo);
    }

    void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() == true) BackupBox.Text = dlg.FolderName;
    }

    void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(HotelNameBox.Text)) { Msg(L.T("Hotel name is required."), true); return; }
        if (!int.TryParse(SlotCountBox.Text.Trim(), out var slots) || slots < 1 || slots > 500)
        { Msg(L.T("Slot count must be between 1 and 500."), true); return; }
        if (!Uri.TryCreate(ReaderBox.Text.Trim(), UriKind.Absolute, out _)) { Msg(L.T("Card reader URL is not valid."), true); return; }

        using (var db = App.OpenDb())
        {
            var s = new SettingsStore(db);
            s.Set(SettingsStore.HotelName, HotelNameBox.Text.Trim());
            if (newLogo != null) s.Set(SettingsStore.LogoPath, App.CopyLogo(newLogo));
            s.Set(SettingsStore.BadgeEnabled, BadgeBox.IsChecked == true ? "1" : "0");
            s.Set(SettingsStore.SlotEnabled, SlotBox.IsChecked == true ? "1" : "0");
            s.Set(SettingsStore.SlotCount, slots.ToString());
            s.Set(SettingsStore.BackupFolder, Str.OrNull(BackupBox.Text));
            s.Set(SettingsStore.ReaderUrl, ReaderBox.Text.Trim());
        }
        newLogo = null;
        App.RunBackup(); // runs now so the banner clears (or shows the error) right away
        (Window.GetWindow(this) as MainWindow)?.RefreshHeader();
        Msg(L.T("Settings saved."), false);
    }

    void LocationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LocationsList.SelectedItem is not Location loc) return;
        LocNameBox.Text = loc.Name;
        LocNameArBox.Text = loc.NameAr ?? "";
    }

    /// Location edits save immediately; the Gate tab reloads them when it is shown.
    void WithSelected(Action<GateDb, Location> change)
    {
        if (LocationsList.SelectedItem is not Location selected) { Msg(L.T("Select a location first."), true); return; }
        using var db = App.OpenDb();
        change(db, db.Locations.Find(selected.Id)!);
        db.SaveChanges();
        LoadLocations(db);
    }

    void AddLocation_Click(object sender, RoutedEventArgs e)
    {
        if (Str.OrNull(LocNameBox.Text) is not string name) { Msg(L.T("Location name is required."), true); return; }
        using var db = App.OpenDb();
        db.Locations.Add(new Location
        {
            Name = name,
            NameAr = Str.OrNull(LocNameArBox.Text),
            SortOrder = (db.Locations.Max(l => (int?)l.SortOrder) ?? -1) + 1,
        });
        db.SaveChanges();
        LoadLocations(db);
        LocNameBox.Text = LocNameArBox.Text = "";
    }

    void RenameLocation_Click(object sender, RoutedEventArgs e)
    {
        if (Str.OrNull(LocNameBox.Text) is not string name) { Msg(L.T("Location name is required."), true); return; }
        WithSelected((_, loc) => { loc.Name = name; loc.NameAr = Str.OrNull(LocNameArBox.Text); });
    }

    void Move(int direction) => WithSelected((db, loc) =>
    {
        var list = db.Locations.OrderBy(l => l.SortOrder).ToList();
        var i = list.FindIndex(l => l.Id == loc.Id);
        var j = i + direction;
        if (j < 0 || j >= list.Count) return;
        (list[i], list[j]) = (list[j], list[i]);
        for (var k = 0; k < list.Count; k++) list[k].SortOrder = k;
    });

    void Up_Click(object sender, RoutedEventArgs e) => Move(-1);
    void Down_Click(object sender, RoutedEventArgs e) => Move(1);
    void ToggleLocation_Click(object sender, RoutedEventArgs e) => WithSelected((_, loc) => loc.IsActive = !loc.IsActive);
}
