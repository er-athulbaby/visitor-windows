using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ServiceGatePass.Services;

namespace ServiceGatePass.Views;

public record DayBar(double Height, Brush Fill, string Tip);
public record TopRow(string Name, int Count, int Max);

public partial class DashboardView : UserControl
{
    const double ChartHeight = 190; // the chart row is 200 high

    public DashboardView() => InitializeComponent();

    void View_Loaded(object sender, RoutedEventArgs e)
    {
        DashboardData d;
        using (var db = App.OpenDb()) d = DashboardQuery.Load(db, DateTime.Today);

        TodayText.Text = d.VisitsToday.ToString();
        InsideText.Text = HeldText.Text = d.InsideNow.ToString(); // every visit inside holds one CPR card
        MonthText.Text = d.PerDay.Sum(x => x.Count).ToString();
        TopLocations.ItemsSource = Rows(d.TopLocations);
        TopCompanies.ItemsSource = Rows(d.TopCompanies);

        var max = Math.Max(1, d.PerDay.Max(x => x.Count));
        var today = (Brush)FindResource("AccentBrush");
        var other = (Brush)FindResource("ChartBarBrush");
        Bars.ItemsSource = d.PerDay.Select((x, i) => new DayBar(
            Math.Max(2, ChartHeight * x.Count / max),
            i == d.PerDay.Count - 1 ? today : other,
            $"{x.Day:dd'/'MM} · {string.Format(L.T("{0} visits"), x.Count)}")).ToList();
        FirstDay.Text = d.PerDay[0].Day.ToString("dd'/'MM");
        MidDay.Text = d.PerDay[d.PerDay.Count / 2].Day.ToString("dd'/'MM");
    }

    static List<TopRow> Rows(List<NameCount> list)
    {
        var max = list.Count == 0 ? 1 : list.Max(x => x.Count);
        return list.Select(x => new TopRow(x.Name, x.Count, max)).ToList();
    }
}
