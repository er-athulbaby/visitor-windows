using ServiceGatePass.Data;
using ServiceGatePass.Services;

namespace ServiceGatePass.Tests;

public class HistoryQueryTests
{
    static (GateDb db, Location kitchen, Location ballroom) Seed()
    {
        var db = TestDb.Create();
        db.Users.Add(new User { Id = 1, Username = "guard", DisplayName = "Guard One", PasswordHash = "x" });
        db.SaveChanges();
        var kitchen = TestDb.AddLocation(db, "Kitchen");
        var ballroom = TestDb.AddLocation(db, "Ballroom");

        void At(DateTime t, string cpr, string name, Location loc, string? company)
        {
            var visit = new GateService(db, () => t).CheckIn(new(cpr, name, null, null, loc.Id, company, null, null, null, 1));
            new GateService(db, () => t.AddHours(1)).CheckOut(visit.Id, 1);
        }

        At(new DateTime(2026, 11, 17, 10, 0, 0), "900000001", "Ali Hasan", kitchen, "Cool Air Co");
        At(new DateTime(2026, 11, 18, 10, 0, 0), "900000002", "Sara Ahmed", ballroom, "Fresh Foods");
        At(new DateTime(2026, 11, 19, 23, 30, 0), "900000003", "John Smith", kitchen, null);
        return (db, kitchen, ballroom);
    }

    static HistoryFilter Range(string? search = null, int? loc = null, string? company = null) =>
        new(new DateTime(2026, 11, 17), new DateTime(2026, 11, 19), search, loc, company);

    [Fact]
    public void Date_range_is_inclusive_and_newest_first()
    {
        var (db, _, _) = Seed();
        Assert.Equal(new[] { "John Smith", "Sara Ahmed", "Ali Hasan" }, HistoryQuery.Run(db, Range()).Select(r => r.Name));
        Assert.Single(HistoryQuery.Run(db, new(new DateTime(2026, 11, 18), new DateTime(2026, 11, 18), null, null, null)));
    }

    [Fact]
    public void Search_matches_name_case_insensitively_and_cpr()
    {
        var (db, _, _) = Seed();
        Assert.Equal("Ali Hasan", HistoryQuery.Run(db, Range(search: "ali")).Single().Name);
        Assert.Equal("Sara Ahmed", HistoryQuery.Run(db, Range(search: "900000002")).Single().Name);
    }

    [Fact]
    public void Filters_by_location_and_company()
    {
        var (db, kitchen, _) = Seed();
        Assert.Equal(2, HistoryQuery.Run(db, Range(loc: kitchen.Id)).Count);
        Assert.Equal("Sara Ahmed", HistoryQuery.Run(db, Range(company: "fresh")).Single().Name);
    }

    [Fact]
    public void Rows_carry_location_times_and_user_names()
    {
        var (db, _, _) = Seed();
        var row = HistoryQuery.Run(db, Range(search: "Ali")).Single();
        Assert.Equal("Kitchen", row.Location);
        Assert.Equal(new DateTime(2026, 11, 17, 11, 0, 0), row.CheckOutAt);
        Assert.Equal("Guard One", row.CheckInBy);
        Assert.Equal("Guard One", row.CheckOutBy);
    }
}
