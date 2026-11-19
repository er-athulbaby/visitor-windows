using Microsoft.EntityFrameworkCore;
using ServiceGatePass.Data;

namespace ServiceGatePass.Tests;

public class DatabaseRulesTests
{
    static Visit Open(Visitor v, Location l, int? badge = null, int? slot = null) =>
        new() { Visitor = v, Location = l, CheckInAt = new DateTime(2026, 11, 19, 9, 0, 0), BadgeNo = badge, SlotNo = slot };

    [Fact]
    public void Second_open_visit_for_same_visitor_is_rejected()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        var v = new Visitor { Cpr = "900000001", NameEn = "Ali" };
        db.Visits.Add(Open(v, loc));
        db.SaveChanges();

        db.Visits.Add(Open(v, loc));
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void Closed_visit_does_not_block_a_new_one()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        var v = new Visitor { Cpr = "900000001", NameEn = "Ali" };
        var first = Open(v, loc);
        first.CheckOutAt = first.CheckInAt.AddHours(1);
        db.Visits.Add(first);
        db.SaveChanges();

        db.Visits.Add(Open(v, loc));
        db.SaveChanges();

        Assert.Equal(2, db.Visits.Count());
    }

    [Fact]
    public void Badge_in_use_is_rejected()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        db.Visits.Add(Open(new Visitor { Cpr = "900000001", NameEn = "A" }, loc, badge: 5));
        db.SaveChanges();

        db.Visits.Add(Open(new Visitor { Cpr = "900000002", NameEn = "B" }, loc, badge: 5));
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void Slot_in_use_is_rejected()
    {
        using var db = TestDb.Create();
        var loc = TestDb.AddLocation(db, "Kitchen");
        db.Visits.Add(Open(new Visitor { Cpr = "900000001", NameEn = "A" }, loc, slot: 3));
        db.SaveChanges();

        db.Visits.Add(Open(new Visitor { Cpr = "900000002", NameEn = "B" }, loc, slot: 3));
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void Visitor_cpr_is_unique()
    {
        using var db = TestDb.Create();
        db.Visitors.Add(new Visitor { Cpr = "900000001", NameEn = "A" });
        db.Visitors.Add(new Visitor { Cpr = "900000001", NameEn = "B" });
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }
}
