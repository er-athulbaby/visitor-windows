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
        w.WriteLine("Name,CPR,Mobile,Vehicle,Location,Company,Note,Badge,Slot,Check-in,Check-out,Checked in by,Checked out by");
        foreach (var r in rows)
            w.WriteLine(string.Join(",", new[]
            {
                Cell(r.Name), CprCell(r.Cpr), Cell(r.Mobile), Cell(r.VehicleNo), Cell(r.Location), Cell(r.Company), Cell(r.Note), Cell(r.BadgeNo?.ToString()), Cell(r.SlotNo?.ToString()),
                Cell(r.CheckInAt.ToString(Time)), Cell(r.CheckOutAt?.ToString(Time)), Cell(r.CheckInBy), Cell(r.CheckOutBy),
            }));
    }

    /// Excel drops leading zeros; a 9-digit CPR is written as ="050000001" so it stays text.
    public static string CprCell(string? v) =>
        v is { Length: 9 } && v.All(char.IsAsciiDigit) ? "=\"" + v + "\"" : Cell(v);

    public static string Cell(string? value)
    {
        var v = value ?? "";
        // A leading = + - @ tab or CR would make Excel run the cell as a formula.
        if (v.Length > 0 && "=+-@\t\r".Contains(v[0])) v = "'" + v;
        return v.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
    }
}
