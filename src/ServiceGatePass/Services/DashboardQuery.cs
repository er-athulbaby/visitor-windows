using Microsoft.EntityFrameworkCore;
using ServiceGatePass.Data;

namespace ServiceGatePass.Services;

public record DayCount(DateTime Day, int Count);
public record NameCount(string Name, int Count);
public record DashboardData(int VisitsToday, int InsideNow, List<DayCount> PerDay, List<NameCount> TopLocations, List<NameCount> TopCompanies);

public static class DashboardQuery
{
    public static DashboardData Load(GateDb db, DateTime today)
    {
        var day = today.Date;
        var from = day.AddDays(-29);
        // ponytail: groups 30 days of visits in memory; fine for a single gate (thousands of rows). Move grouping to SQL if it ever gets slow.
        var visits = db.Visits.Include(v => v.Location).Where(v => v.CheckInAt >= from).ToList();

        var perDay = Enumerable.Range(0, 30).Select(i => from.AddDays(i))
            .Select(d => new DayCount(d, visits.Count(v => v.CheckInAt.Date == d))).ToList();

        static List<NameCount> Top(IEnumerable<string> names) =>
            names.GroupBy(n => n).Select(g => new NameCount(g.Key, g.Count()))
                .OrderByDescending(x => x.Count).ThenBy(x => x.Name).Take(5).ToList();

        return new DashboardData(
            visits.Count(v => v.CheckInAt.Date == day),
            db.Visits.Count(v => v.CheckOutAt == null),
            perDay,
            Top(visits.Select(v => v.Location.Display)),
            Top(visits.Select(v => Str.OrNull(v.Company)).OfType<string>()));
    }
}
