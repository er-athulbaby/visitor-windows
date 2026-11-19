using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ServiceGatePass.Services;

namespace ServiceGatePass.Views;

public record LocationItem(int Id, string Display);

/// Picks one of four soft avatar colours from the initials, so a name always gets the same one.
/// ConverterParameter "fg" returns the text colour, anything else the background.
public class AvatarBrushConverter : IValueConverter
{
    static readonly (Brush Bg, Brush Fg)[] Palette =
    [
        (Hex("#CFE9EA"), Hex("#0A4F54")), (Hex("#E7ECF6"), Hex("#23406E")),
        (Hex("#F3E8F5"), Hex("#6B2E78")), (Hex("#FFF1DD"), Hex("#7A4B00")),
    ];

    static Brush Hex(string c)
    {
        var b = (Brush)new BrushConverter().ConvertFromString(c)!;
        b.Freeze();
        return b;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var pair = Palette[(value as string ?? "").Sum(ch => ch) % Palette.Length];
        return parameter as string == "fg" ? pair.Fg : pair.Bg;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public partial class GateView : UserControl
{
    readonly DispatcherTimer refresh = new() { Interval = TimeSpan.FromMinutes(1) };
    CardReader reader = null!;
    bool badgeOn, slotOn;
    int slotCount;
    string? nameAr, employer;
    List<InsideRow> inside = [];

    public GateView()
    {
        InitializeComponent();
        refresh.Tick += (_, _) => LoadInside();
        // Settings and locations may have changed on the admin tabs; reload whenever this tab shows.
        Loaded += (_, _) => { LoadSettings(); LoadInside(); refresh.Start(); Cpr.Focus(); };
        Unloaded += (_, _) => refresh.Stop();
        LoadSettings();
        ClearForm();
    }

    void LoadSettings()
    {
        using var db = App.OpenDb();
        var s = new SettingsStore(db);
        reader = new CardReader(App.Http, s.Get(SettingsStore.ReaderUrl) ?? CardReader.DefaultUrl);
        badgeOn = s.GetBool(SettingsStore.BadgeEnabled);
        slotOn = s.GetBool(SettingsStore.SlotEnabled);
        slotCount = s.GetInt(SettingsStore.SlotCount, 50);
        BadgePanel.Visibility = BadgeColumn.Visibility = badgeOn ? Visibility.Visible : Visibility.Collapsed;
        SlotPanel.Visibility = SlotColumn.Visibility = slotOn ? Visibility.Visible : Visibility.Collapsed;

        var selected = Locations.SelectedValue;
        Locations.ItemsSource = db.Locations.Where(l => l.IsActive).OrderBy(l => l.SortOrder).ToList()
            .Select(l => new LocationItem(l.Id, l.Display)).ToList();
        Locations.SelectedValue = selected;
        RefreshSlots(keepSelection: true);
    }

    void LoadInside()
    {
        using var db = App.OpenDb();
        inside = new GateService(db).Inside();
        InsideCount.Text = CardsHeldCount.Text = inside.Count.ToString();
        VisitsTodayCount.Text = db.Visits.Count(v => v.CheckInAt >= DateTime.Today).ToString();
        DateLine.Text = DateTime.Now.ToString("dddd d MMMM · HH:mm",
            L.IsArabic ? CultureInfo.GetCultureInfo("ar-BH") : CultureInfo.GetCultureInfo("en-GB"));
        ApplySearch();
    }

    void ApplySearch()
    {
        var selectedId = (InsideGrid.SelectedItem as InsideRow)?.VisitId;
        var q = Search.Text.Trim();
        var rows = q == ""
            ? inside
            : inside.Where(r => r.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || r.Cpr.Contains(q)
                || r.BadgeNo?.ToString() == q || r.SlotNo?.ToString() == q).ToList();
        InsideGrid.ItemsSource = rows;
        InsideGrid.SelectedItem = rows.FirstOrDefault(r => r.VisitId == selectedId) ?? (rows.Count == 1 ? rows[0] : null);
    }

    void RefreshSlots(bool keepSelection)
    {
        if (!slotOn) { Slot.ItemsSource = null; return; }
        using var db = App.OpenDb();
        var free = new GateService(db).FreeSlots(slotCount);
        var current = keepSelection ? Slot.SelectedItem as int? : null;
        Slot.ItemsSource = free;
        Slot.SelectedItem = current is int c && free.Contains(c) ? c : free.Count > 0 ? free[0] : null;
    }

    void ClearForm()
    {
        Cpr.Text = NameEn.Text = Company.Text = Note.Text = Badge.Text = NameArText.Text = ReaderMessage.Text = "";
        nameAr = employer = null;
        Photo.Source = null;
        ReturningChip.Visibility = Visibility.Collapsed;
        Locations.SelectedIndex = -1;
        RefreshSlots(keepSelection: false);
    }

    async void ReadCard_Click(object? sender, RoutedEventArgs? e)
    {
        ClearForm();
        ReadButton.IsEnabled = false;
        ReaderMessage.Text = L.T("Reading card…");
        try
        {
            var result = await reader.ReadAsync();
            if (result.Card is not { } card)
            {
                ReaderMessage.Text = result.Error ?? "";
                Cpr.Focus();
                return;
            }
            ReaderMessage.Text = "";
            Cpr.Text = card.Cpr;
            NameEn.Text = card.NameEn;
            nameAr = card.NameAr;
            NameArText.Text = nameAr ?? "";
            employer = card.Employer;
            Photo.Source = App.ImageFromBase64(card.PhotoBase64); // display only, never stored
            Prefill(fromCard: true);
            Locations.Focus();
        }
        finally { ReadButton.IsEnabled = true; }
    }

    /// Returning visitor: fill from their last visit. A fresh card read keeps the card's name.
    void Prefill(bool fromCard)
    {
        using var db = App.OpenDb();
        var visitor = new GateService(db).FindVisitor(Cpr.Text);
        if (visitor == null)
        {
            Company.Text = employer ?? "";
            return;
        }
        ReturningText.Text = string.Format(L.T("Returning visitor · {0} visits"), db.Visits.Count(v => v.VisitorId == visitor.Id));
        ReturningChip.Visibility = Visibility.Visible;
        if (!fromCard)
        {
            NameEn.Text = visitor.NameEn;
            nameAr = visitor.NameAr;
            NameArText.Text = nameAr ?? "";
            employer = visitor.Employer;
        }
        Company.Text = visitor.LastCompany ?? employer ?? "";
        Note.Text = visitor.LastNote ?? "";
        if (visitor.LastLocationId is int id) Locations.SelectedValue = id;
    }

    /// Name, Arabic name, employer and photo belong to the previous card once the CPR is edited.
    void Cpr_TextChanged(object sender, TextChangedEventArgs e)
    {
        nameAr = employer = null;
        NameArText.Text = "";
        Photo.Source = null;
        ReturningChip.Visibility = Visibility.Collapsed;
    }

    void Cpr_LostFocus(object sender, RoutedEventArgs e)
    {
        if (Cpr.Text.Trim().Length == 9 && NameEn.Text.Trim() == "") Prefill(fromCard: false);
    }

    void CheckIn_Click(object? sender, RoutedEventArgs? e)
    {
        if (slotOn && Slot.SelectedItem is null) Show(CheckInMessage, L.T("All card slots are full."), error: true);
        int? badge = null, usedSlot = null;
        if (badgeOn && Badge.Text.Trim() != "")
        {
            if (!int.TryParse(Badge.Text.Trim(), out var b) || b <= 0)
            {
                Show(CheckInMessage, L.T("Badge # must be a number."), error: true);
                return;
            }
            badge = b;
        }

        try
        {
            using var db = App.OpenDb();
            usedSlot = slotOn ? Slot.SelectedItem as int? : null;
            new GateService(db).CheckIn(new CheckInRequest(Cpr.Text, NameEn.Text, nameAr, employer,
                Locations.SelectedValue as int? ?? 0, Company.Text, Note.Text, badge,
                usedSlot, App.CurrentUser.Id));
        }
        catch (GateException ex)
        {
            Show(CheckInMessage, ex.Message, error: true);
            return;
        }

        var name = NameEn.Text.Trim();
        ClearForm();
        LoadInside();
        Show(CheckInMessage, usedSlot is int s
            ? string.Format(L.T("{0} checked in. Put the CPR card in slot {1}."), name, s)
            : string.Format(L.T("{0} checked in."), name), error: false);
        Cpr.Focus();
    }

    void CheckOut(InsideRow row)
    {
        try
        {
            using var db = App.OpenDb();
            new GateService(db).CheckOut(row.VisitId, App.CurrentUser.Id);
        }
        catch (GateException ex)
        {
            Show(CheckOutMessage, ex.Message, error: true);
            LoadInside();
            return;
        }

        Search.Text = "";
        LoadInside();
        RefreshSlots(keepSelection: true);
        Show(CheckOutMessage, row.SlotNo is int slot
            ? string.Format(L.T("{0} checked out. Return CPR card from slot {1}."), row.Name, slot)
            : string.Format(L.T("{0} checked out. Return the CPR card."), row.Name), error: false);
        Search.Focus();
    }

    void CheckOut_Click(object sender, RoutedEventArgs e)
    {
        if (InsideGrid.SelectedItem is InsideRow row) CheckOut(row);
        else Show(CheckOutMessage, L.T("Select a person in the list first."), error: true);
    }

    void Search_TextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

    void Search_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Search.Text.Trim() != "" && InsideGrid.ItemsSource is List<InsideRow> { Count: 1 } rows)
        {
            CheckOut(rows[0]);
            e.Handled = true;
        }
    }

    void CheckInPanel_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        CheckIn_Click(null, null);
        e.Handled = true;
    }

    void View_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2)
        {
            if (ReadButton.IsEnabled) ReadCard_Click(null, null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ClearForm();
            Cpr.Focus();
            e.Handled = true;
        }
    }

    static void Show(TextBlock target, string message, bool error)
    {
        target.Text = message;
        target.Tag = error ? "error" : "ok";
        target.Foreground = (Brush)target.FindResource(error ? "ErrorBrush" : "SuccessBrush");
    }
}
