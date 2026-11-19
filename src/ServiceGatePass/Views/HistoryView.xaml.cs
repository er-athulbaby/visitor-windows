using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using ServiceGatePass.Services;

namespace ServiceGatePass.Views;

public partial class HistoryView : UserControl
{
    const string Time = "yyyy-MM-dd HH:mm";
    List<HistoryRow> rows = [];

    public HistoryView()
    {
        InitializeComponent();
        FromDate.SelectedDate = ToDate.SelectedDate = DateTime.Today;
        using var db = App.OpenDb();
        var locations = new List<LocationItem> { new(0, L.T("All locations")) };
        locations.AddRange(db.Locations.OrderBy(l => l.SortOrder).ToList().Select(l => new LocationItem(l.Id, l.Display)));
        LocationBox.ItemsSource = locations;
        LocationBox.SelectedIndex = 0;
        Run();
    }

    void Run()
    {
        using var db = App.OpenDb();
        rows = HistoryQuery.Run(db, new HistoryFilter(
            FromDate.SelectedDate ?? DateTime.Today,
            ToDate.SelectedDate ?? DateTime.Today,
            SearchBox.Text,
            LocationBox.SelectedValue is int id && id != 0 ? id : null,
            CompanyBox.Text));
        ResultsGrid.ItemsSource = rows;
        CountText.Text = string.Format(L.T("{0} visits"), rows.Count);
    }

    void Show_Click(object sender, RoutedEventArgs e) => Run();

    void Export_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"gate-history-{DateTime.Now:yyyyMMdd-HHmm}.csv" };
        if (dlg.ShowDialog() != true) return;
        using var file = File.Create(dlg.FileName);
        CsvExport.Write(file, rows);
    }

    void Print_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;
        string hotel;
        using (var db = App.OpenDb()) hotel = new SettingsStore(db).Get(SettingsStore.HotelName) ?? "";
        var title = $"{hotel} — {L.T("Gate history")} {FromDate.SelectedDate:yyyy-MM-dd} – {ToDate.SelectedDate:yyyy-MM-dd}";
        var doc = BuildDocument(title, dlg.PrintableAreaWidth);
        dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, L.T("Gate history"));
    }

    FlowDocument BuildDocument(string title, double width)
    {
        var doc = new FlowDocument
        {
            PageWidth = width,
            ColumnWidth = width,
            FontSize = 11,
            PagePadding = new Thickness(40),
            FlowDirection = L.IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
        };
        doc.Blocks.Add(new Paragraph(new Run(title)) { FontSize = 16, FontWeight = FontWeights.Bold });

        string[] headers = [L.T("Name"), L.T("CPR"), L.T("Location"), L.T("Company"), L.T("In"), L.T("Out")];
        var table = new Table { CellSpacing = 0 };
        foreach (var _ in headers) table.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        group.Rows.Add(Row(headers, bold: true));
        foreach (var r in rows)
            group.Rows.Add(Row([r.Name, r.Cpr, r.Location, r.Company ?? "", r.CheckInAt.ToString(Time),
                r.CheckOutAt?.ToString(Time) ?? L.T("Inside")], bold: false));
        doc.Blocks.Add(table);
        return doc;

        static TableRow Row(string[] cells, bool bold)
        {
            var row = new TableRow();
            foreach (var cell in cells)
                row.Cells.Add(new TableCell(new Paragraph(new Run(cell)))
                {
                    FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                    Padding = new Thickness(2),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0, 0, 0, 0.5),
                });
            return row;
        }
    }
}
