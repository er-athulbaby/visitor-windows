using System.IO;
using System.Text;

namespace ServiceGatePass.Services;

public static class CsvExport
{
    const string Time = "yyyy-MM-dd HH:mm";

    public static void Write(Stream stream, IEnumerable<HistoryRow> rows)
    {
        // BOM so Excel shows Arabic names correctly when the file is double-clicked.
        using var w = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);
        w.NewLine = "\r\n";
        w.WriteLine("Name,CPR,Location,Company,Note,Badge,Slot,Check-in,Check-out,Checked in by,Checked out by");
        foreach (var r in rows)
            w.WriteLine(string.Join(",", new[]
            {
                r.Name, r.Cpr, r.Location, r.Company, r.Note, r.BadgeNo?.ToString(), r.SlotNo?.ToString(),
                r.CheckInAt.ToString(Time), r.CheckOutAt?.ToString(Time), r.CheckInBy, r.CheckOutBy,
            }.Select(Cell)));
    }

    public static string Cell(string? value)
    {
        var v = value ?? "";
        // A leading = + - @ tab or CR would make Excel run the cell as a formula.
        if (v.Length > 0 && "=+-@\t\r".Contains(v[0])) v = "'" + v;
        return v.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
    }
}
