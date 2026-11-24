using Microsoft.EntityFrameworkCore;
using ServiceGatePass.Data;

namespace ServiceGatePass.Services;

public record CheckInRequest(string Cpr, string NameEn, string? NameAr, string? Employer, int LocationId,
    string? Company, string? Note, int? BadgeNo, int? SlotNo, int UserId, string? Mobile = null, string? VehicleNo = null);

public record InsideRow(int VisitId, string Name, string Cpr, string Location, DateTime CheckInAt, int? BadgeNo, int? SlotNo,
    string? Company, string? VehicleNo = null)
{
    public string Initials => InitialsOf(Name);

    /// First letters of the first two words, upper-case ("Ali hasan Ahmed" → "AH").
    public static string InitialsOf(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpperInvariant(w[0])));

    public string Duration
    {
        get
        {
            var d = DateTime.Now - CheckInAt;
            return $"{(int)d.TotalHours}h {d.Minutes:00}m";
        }
    }
}

public class GateService(GateDb db, Func<DateTime>? clock = null)
{
    readonly Func<DateTime> now = clock ?? (() => DateTime.Now);

    public Visitor? FindVisitor(string cpr)
    {
        var c = cpr.Trim();
        return db.Visitors.SingleOrDefault(v => v.Cpr == c);
    }

    /// Earlier visitors whose CPR starts with what has been typed so far, most recent first.
    public List<Visitor> SuggestVisitors(string cprStart, int max = 6)
    {
        var p = cprStart.Trim();
        if (p.Length < 3) return [];
        return db.Visitors.Where(v => v.Cpr.StartsWith(p)).OrderByDescending(v => v.UpdatedAt).Take(max).ToList();
    }

    /// Vehicle number from the visitor's most recent visit that had one, to refill the form.
    public string? LastVehicle(int visitorId) =>
        db.Visits.Where(v => v.VisitorId == visitorId && v.VehicleNo != null)
            .OrderByDescending(v => v.CheckInAt).Select(v => v.VehicleNo).FirstOrDefault();

    public Visit CheckIn(CheckInRequest r)
    {
        var cpr = r.Cpr.Trim();
        if (cpr.Length != 9 || !cpr.All(char.IsAsciiDigit)) throw new GateException(L.T("CPR must be 9 digits."));
        if (string.IsNullOrWhiteSpace(r.NameEn)) throw new GateException(L.T("Name is required."));
        var location = db.Locations.SingleOrDefault(l => l.Id == r.LocationId && l.IsActive)
            ?? throw new GateException(L.T("Pick a location."));

        // Friendly checks first; the partial unique indexes in GateDb are the backstop.
        var open = db.Visits.Where(v => v.CheckOutAt == null);
        var already = open.Include(v => v.Location).FirstOrDefault(v => v.Visitor.Cpr == cpr);
        if (already != null)
            throw new GateException(string.Format(L.T("Already inside at {0} since {1}."),
                already.Location.Display, already.CheckInAt.ToString("HH:mm")));
        if (r.BadgeNo is int badge && open.Any(v => v.BadgeNo == badge))
            throw new GateException(string.Format(L.T("Badge {0} is already in use."), badge));
        if (r.SlotNo is int slot && open.Any(v => v.SlotNo == slot))
            throw new GateException(string.Format(L.T("Slot {0} is already in use."), slot));

        var t = now();
        var visitor = FindVisitor(cpr);
        if (visitor == null)
        {
            visitor = new Visitor { Cpr = cpr, CreatedAt = t };
            db.Visitors.Add(visitor);
        }
        visitor.NameEn = r.NameEn.Trim();
        visitor.NameAr = Str.OrNull(r.NameAr) ?? visitor.NameAr;
        visitor.Employer = Str.OrNull(r.Employer) ?? visitor.Employer;
        visitor.Mobile = Str.OrNull(r.Mobile) ?? visitor.Mobile;
        visitor.LastCompany = Str.OrNull(r.Company);
        visitor.LastLocationId = location.Id;
        visitor.LastNote = Str.OrNull(r.Note);
        visitor.UpdatedAt = t;

        var visit = new Visit
        {
            Visitor = visitor,
            Location = location,
            Company = Str.OrNull(r.Company),
            Note = Str.OrNull(r.Note),
            VehicleNo = Str.OrNull(r.VehicleNo)?.ToUpperInvariant(),
            BadgeNo = r.BadgeNo,
            SlotNo = r.SlotNo,
            CheckInAt = t,
            CheckInByUserId = r.UserId,
        };
        db.Visits.Add(visit);
        db.SaveChanges();
        return visit;
    }

    public void CheckOut(int visitId, int userId)
    {
        var visit = db.Visits.Find(visitId) ?? throw new GateException(L.T("Visit not found."));
        if (visit.CheckOutAt != null) throw new GateException(L.T("Already checked out."));
        visit.CheckOutAt = now();
        visit.CheckOutByUserId = userId;
        db.SaveChanges();
    }

    public List<InsideRow> Inside() =>
        db.Visits.Include(v => v.Visitor).Include(v => v.Location)
            .Where(v => v.CheckOutAt == null)
            .OrderBy(v => v.CheckInAt)
            .AsEnumerable()
            .Select(v => new InsideRow(v.Id, v.Visitor.NameEn, v.Visitor.Cpr, v.Location.Display, v.CheckInAt, v.BadgeNo, v.SlotNo, v.Company, v.VehicleNo))
            .ToList();

    public List<int> FreeSlots(int slotCount)
    {
        var used = db.Visits.Where(v => v.CheckOutAt == null && v.SlotNo != null).Select(v => v.SlotNo!.Value).ToHashSet();
        return Enumerable.Range(1, slotCount).Where(s => !used.Contains(s)).ToList();
    }
}
