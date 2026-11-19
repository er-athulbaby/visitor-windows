using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServiceGatePass.Data;

public class GateDb(DbContextOptions<GateDb> options) : DbContext(options)
{
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Setting> Settings => Set<Setting>();

    public static DbContextOptions<GateDb> OptionsFor(string path) =>
        new DbContextOptionsBuilder<GateDb>().UseSqlite($"Data Source={path}").Options;

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Visitor>().HasIndex(v => v.Cpr).IsUnique();

        // "Inside now" = CheckOutAt IS NULL. These partial indexes make the database itself
        // refuse a second open visit per person, badge or slot.
        b.Entity<Visit>().HasIndex(v => v.VisitorId).IsUnique().HasFilter("CheckOutAt IS NULL");
        b.Entity<Visit>().HasIndex(v => v.BadgeNo).IsUnique().HasFilter("CheckOutAt IS NULL AND BadgeNo IS NOT NULL");
        b.Entity<Visit>().HasIndex(v => v.SlotNo).IsUnique().HasFilter("CheckOutAt IS NULL AND SlotNo IS NOT NULL");
        b.Entity<Visit>().HasIndex(v => v.CheckInAt);
        b.Entity<Visit>().HasOne(v => v.Location).WithMany().HasForeignKey(v => v.LocationId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<User>().HasIndex(u => u.Username).IsUnique();
        b.Entity<Setting>().HasKey(s => s.Key);
    }
}

// Used only by `dotnet ef` at design time.
public class GateDbFactory : IDesignTimeDbContextFactory<GateDb>
{
    public GateDb CreateDbContext(string[] args) => new(GateDb.OptionsFor("design.db"));
}
