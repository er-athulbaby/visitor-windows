using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using ServiceGatePass.Services;

namespace ServiceGatePass.Views;

public partial class DashboardView : UserControl
{
    public DashboardView() => InitializeComponent();

    void View_Loaded(object sender, RoutedEventArgs e)
    {
        DashboardData d;
        using (var db = App.OpenDb()) d = DashboardQuery.Load(db, DateTime.Today);

        TodayText.Text = d.VisitsToday.ToString();
        InsideText.Text = d.InsideNow.ToString();
        TopLocations.ItemsSource = d.TopLocations;
        TopCompanies.ItemsSource = d.TopCompanies;
        Chart.Series = [new ColumnSeries<int> { Values = d.PerDay.Select(x => x.Count).ToArray(), Name = L.T("Visits") }];
        Chart.XAxes = [new Axis { Labels = d.PerDay.Select(x => x.Day.ToString("dd/MM")).ToArray(), LabelsRotation = 45 }];
        Chart.YAxes = [new Axis { MinLimit = 0 }];
    }
}
