using ServiceGatePass.Data;
using ServiceGatePass.Services;

namespace ServiceGatePass.Tests;

public class GateServiceTests
{
    static readonly DateTime T0 = new(2026, 11, 19, 9, 12, 0);
    DateTime now = T0;

    GateService Gate(GateDb db) => new(db, () => now);

    static CheckInRequest Req(string cpr, int locationId, string name = "Ali Hasan", int? badge = null, int? slot = null,
        string? company = null, string? note = null) =>
        new(cpr, name, null, "ACME WLL", locationId, company, note, badge, slot, UserId: 1);

    [Fact]
    public void CheckIn_creates_visitor_and_open_visit()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");

        var visit = Gate(db).CheckIn(Req(" 900000001 ", loc.Id));

        Assert.Null(visit.CheckOutAt);
        Assert.Equal(T0, visit.CheckInAt);
        var visitor = db.Visitors.Single();
        Assert.Equal("900000001", visitor.Cpr);
        Assert.Equal("Ali Hasan", visitor.NameEn);
        Assert.Equal("ACME WLL", visitor.Employer);
        Assert.Single(Gate(db).Inside());
    }

    [Fact]
    public void CheckIn_refuses_visitor_already_inside()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        Gate(db).CheckIn(Req("900000001", loc.Id));
        now = T0.AddMinutes(30);

        var ex = Assert.Throws<GateException>(() => Gate(db).CheckIn(Req("900000001", loc.Id)));

        Assert.Equal("Already inside at Kitchen since 09:12.", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678")]
    [InlineData("1234567890")]
    [InlineData("12345678A")]
    public void CheckIn_requires_nine_digit_cpr(string cpr)
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        var ex = Assert.Throws<GateException>(() => Gate(db).CheckIn(Req(cpr, loc.Id)));
        Assert.Equal("CPR must be 9 digits.", ex.Message);
    }

    [Fact]
    public void CheckIn_requires_name_and_active_location()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        Assert.Equal("Name is required.",
            Assert.Throws<GateException>(() => Gate(db).CheckIn(Req("900000001", loc.Id, name: " "))).Message);
        Assert.Equal("Pick a location.",
            Assert.Throws<GateException>(() => Gate(db).CheckIn(Req("900000001", 999))).Message);

        loc.IsActive = false;
        db.SaveChanges();
        Assert.Throws<GateException>(() => Gate(db).CheckIn(Req("900000001", loc.Id)));
    }

    [Fact]
    public void Badge_and_slot_in_use_are_refused()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        Gate(db).CheckIn(Req("900000001", loc.Id, badge: 5, slot: 3));

        Assert.Equal("Badge 5 is already in use.",
            Assert.Throws<GateException>(() => Gate(db).CheckIn(Req("900000002", loc.Id, badge: 5))).Message);
        Assert.Equal("Slot 3 is already in use.",
            Assert.Throws<GateException>(() => Gate(db).CheckIn(Req("900000002", loc.Id, slot: 3))).Message);
    }

    [Fact]
    public void CheckOut_records_time_and_user_and_frees_badge_and_slot()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        var visit = Gate(db).CheckIn(Req("900000001", loc.Id, badge: 5, slot: 3));
        now = T0.AddHours(2);

        Gate(db).CheckOut(visit.Id, userId: 7);

        var saved = db.Visits.Single();
        Assert.Equal(T0.AddHours(2), saved.CheckOutAt);
        Assert.Equal(7, saved.CheckOutByUserId);
        Assert.Empty(Gate(db).Inside());
        Gate(db).CheckIn(Req("900000002", loc.Id, badge: 5, slot: 3));
    }

    [Fact]
    public void CheckOut_twice_is_refused()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        var visit = Gate(db).CheckIn(Req("900000001", loc.Id));
        Gate(db).CheckOut(visit.Id, 1);
        Assert.Equal("Already checked out.",
            Assert.Throws<GateException>(() => Gate(db).CheckOut(visit.Id, 1)).Message);
    }

    [Fact]
    public void Returning_visitor_keeps_last_company_location_and_note()
    {
        using var db = TestDb.Create();
        TestDb.AddLocation(db, "Kitchen");
        var ballroom = TestDb.AddLocation(db, "Ballroom");
        var visit = Gate(db).CheckIn(Req("900000001", ballroom.Id, company: "Cool Air Co", note: "AC repair"));
        Gate(db).CheckOut(visit.Id, 1);

        var visitor = Gate(db).FindVisitor("900000001")!;

        Assert.Equal("Cool Air Co", visitor.LastCompany);
        Assert.Equal(ballroom.Id, visitor.LastLocationId);
        Assert.Equal("AC repair", visitor.LastNote);
        Assert.Null(Gate(db).FindVisitor("900000009"));
    }

    [Fact]
    public void FreeSlots_lists_unused_slots_in_order()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        Gate(db).CheckIn(Req("900000001", loc.Id, slot: 1));
        Gate(db).CheckIn(Req("900000002", loc.Id, slot: 3));

        Assert.Equal(new[] { 2, 4 }, Gate(db).FreeSlots(4));
    }

    [Fact]
    public void Inside_lists_only_open_visits_oldest_first()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        var first = Gate(db).CheckIn(Req("900000001", loc.Id, name: "First"));
        now = T0.AddMinutes(5);
        Gate(db).CheckIn(Req("900000002", loc.Id, name: "Second"));
        now = T0.AddMinutes(10);
        var third = Gate(db).CheckIn(Req("900000003", loc.Id, name: "Third"));
        Gate(db).CheckOut(third.Id, 1);

        var rows = Gate(db).Inside();

        Assert.Equal(new[] { "First", "Second" }, rows.Select(r => r.Name));
        Assert.Equal(first.Id, rows[0].VisitId);
        Assert.Equal("Kitchen", rows[0].Location);
    }

    [Fact]
    public void Inside_rows_carry_company_and_initials()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        Gate(db).CheckIn(Req("900000001", loc.Id, name: "ali  hasan Ahmed", company: "Cool Air Co."));
        Gate(db).CheckIn(Req("900000002", loc.Id, name: "Sara"));

        var rows = Gate(db).Inside();

        Assert.Equal("Cool Air Co.", rows[0].Company);
        Assert.Equal("AH", rows[0].Initials);
        Assert.Null(rows[1].Company);
        Assert.Equal("S", rows[1].Initials);
    }

    [Fact]
    public void Mobile_is_optional_and_kept_when_left_blank_later()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        var gate = Gate(db);

        var v1 = gate.CheckIn(Req("900000001", loc.Id) with { Mobile = " 33445566 " });
        Assert.Equal("33445566", db.Visitors.Single().Mobile);
        gate.CheckOut(v1.Id, 1);

        gate.CheckIn(Req("900000001", loc.Id));
        Assert.Equal("33445566", db.Visitors.Single().Mobile);

        gate.CheckIn(Req("900000002", loc.Id));
        Assert.Null(db.Visitors.Single(v => v.Cpr == "900000002").Mobile);
    }

    [Fact]
    public void SuggestVisitors_matches_cpr_start_from_three_digits()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        var gate = Gate(db);
        gate.CheckIn(Req("960934359", loc.Id));
        gate.CheckIn(Req("980445361", loc.Id));

        Assert.Empty(gate.SuggestVisitors("96"));
        Assert.Equal("960934359", Assert.Single(gate.SuggestVisitors("9609")).Cpr);
        Assert.Empty(gate.SuggestVisitors("123"));
    }
}
