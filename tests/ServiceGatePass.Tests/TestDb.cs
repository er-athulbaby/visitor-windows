using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServiceGatePass.Data;

// L (translations) is global state; run tests one at a time so language switches can't leak.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ServiceGatePass.Tests;

public static class TestDb
{
    public static GateDb Create()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var db = new GateDb(new DbContextOptionsBuilder<GateDb>().UseSqlite(conn).Options);
        db.Database.Migrate();
        return db;
    }

    public static Location AddLocation(GateDb db, string name, int sort = 0)
    {
        var location = new Location { Name = name, SortOrder = sort };
        db.Locations.Add(location);
        db.SaveChanges();
        return location;
    }
}
