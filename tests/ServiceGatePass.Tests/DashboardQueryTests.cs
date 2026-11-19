using ServiceGatePass.Services;

namespace ServiceGatePass.Tests;

public class DashboardQueryTests
{
    [Fact]
    public void Loads_counts_per_day_and_top_lists()
    {
        using var db = TestDb.Create();
        var kitchen = TestDb.AddLocation(db, "Kitchen");
        var ballroom = TestDb.AddLocation(db, "Ballroom");
        var today = new DateTime(2026, 11, 19);
        var n = 0;

        int CheckIn(DateTime t, int loc, string? company) =>
            new GateService(db, () => t).CheckIn(new($"9000000{++n:00}", "V" + n, null, null, loc, company, null, null, null, 1)).Id;

        new GateService(db).CheckOut(CheckIn(today.AddDays(-40), kitchen.Id, "Old Co"), 1); // outside 30 days
        new GateService(db).CheckOut(CheckIn(today.AddDays(-1).AddHours(9), kitchen.Id, "Cool Air"), 1);
        CheckIn(today.AddHours(8), kitchen.Id, "Cool Air");
        CheckIn(today.AddHours(9), ballroom.Id, " ");

        var d = DashboardQuery.Load(db, today);

        Assert.Equal(2, d.VisitsToday);
        Assert.Equal(2, d.InsideNow);
        Assert.Equal(30, d.PerDay.Count);
        Assert.Equal(today.AddDays(-29), d.PerDay[0].Day);
        Assert.Equal(new DayCount(today, 2), d.PerDay[^1]);
        Assert.Equal(1, d.PerDay[^2].Count);
        Assert.Equal(new[] { new NameCount("Kitchen", 2), new NameCount("Ballroom", 1) }, d.TopLocations);
        Assert.Equal(new[] { new NameCount("Cool Air", 2) }, d.TopCompanies);
    }
}
