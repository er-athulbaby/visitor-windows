using Microsoft.EntityFrameworkCore;
using ServiceGatePass.Data;

namespace ServiceGatePass.Services;

public record HistoryFilter(DateTime From, DateTime To, string? Search, int? LocationId, string? Company);

public record HistoryRow(string Name, string Cpr, string Location, string? Company, string? Note, int? BadgeNo, int? SlotNo,
    DateTime CheckInAt, DateTime? CheckOutAt, string CheckInBy, string? CheckOutBy, string? Mobile = null);

public static class HistoryQuery
{
    public static List<HistoryRow> Run(GateDb db, HistoryFilter f)
    {
        var from = f.From.Date;
        var to = f.To.Date.AddDays(1);
        var q = db.Visits.Include(v => v.Visitor).Include(v => v.Location)
            .Where(v => v.CheckInAt >= from && v.CheckInAt < to);

        if (Str.OrNull(f.Search) is string s)
        {
            var like = $"%{s}%"; // SQLite LIKE is case-insensitive for ASCII
            q = q.Where(v => EF.Functions.Like(v.Visitor.NameEn, like) || v.Visitor.Cpr.Contains(s)
                || (v.Visitor.NameAr != null && v.Visitor.NameAr.Contains(s)));
        }
        if (f.LocationId is int locationId) q = q.Where(v => v.LocationId == locationId);
        if (Str.OrNull(f.Company) is string c)
        {
            var like = $"%{c}%";
            q = q.Where(v => v.Company != null && EF.Functions.Like(v.Company, like));
        }

        var users = db.Users.ToDictionary(u => u.Id, u => u.DisplayName);
        return q.OrderByDescending(v => v.CheckInAt).AsEnumerable()
            .Select(v => new HistoryRow(v.Visitor.NameEn, v.Visitor.Cpr, v.Location.Display, v.Company, v.Note, v.BadgeNo, v.SlotNo,
                v.CheckInAt, v.CheckOutAt, users.GetValueOrDefault(v.CheckInByUserId, ""),
                v.CheckOutByUserId is int outBy ? users.GetValueOrDefault(outBy, "") : null, v.Visitor.Mobile))
            .ToList();
    }
}
