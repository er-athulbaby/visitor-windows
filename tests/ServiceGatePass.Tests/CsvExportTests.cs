using System.IO;
using System.Text;
using ServiceGatePass.Services;

namespace ServiceGatePass.Tests;

public class CsvExportTests
{
    static readonly HistoryRow Row = new("علي حسن", "900000001", "Kitchen", "=SUM(A1)", "a, \"b\"", 5, 3,
        new DateTime(2026, 11, 19, 9, 12, 0), null, "Guard One", null);

    static byte[] Export()
    {
        using var ms = new MemoryStream();
        CsvExport.Write(ms, [Row]);
        return ms.ToArray();
    }

    [Fact]
    public void Starts_with_utf8_bom_and_keeps_arabic()
    {
        var bytes = Export();
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        Assert.Contains("علي حسن", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Writes_header_and_row()
    {
        var lines = Encoding.UTF8.GetString(Export()).TrimStart('﻿').Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("Name,CPR,Location,Company,Note,Badge,Slot,Check-in,Check-out,Checked in by,Checked out by", lines[0]);
        Assert.Equal("علي حسن,900000001,Kitchen,'=SUM(A1),\"a, \"\"b\"\"\",5,3,2026-11-19 09:12,,Guard One,", lines[1]);
    }

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+973", "'+973")]
    [InlineData("-5", "'-5")]
    [InlineData("@cmd", "'@cmd")]
    [InlineData("\tx", "'\tx")]
    [InlineData("plain", "plain")]
    [InlineData(null, "")]
    public void Cell_blocks_formula_injection(string? input, string expected) =>
        Assert.Equal(expected, CsvExport.Cell(input));
}
