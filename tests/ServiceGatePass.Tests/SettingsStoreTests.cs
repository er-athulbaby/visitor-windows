using ServiceGatePass.Services;

namespace ServiceGatePass.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void Set_then_get_and_overwrite()
    {
        using var db = TestDb.Create();
        var s = new SettingsStore(db);
        s.Set(SettingsStore.HotelName, "Gulf Hotel");
        s.Set(SettingsStore.HotelName, "Gulf Hotel Bahrain");
        Assert.Equal("Gulf Hotel Bahrain", new SettingsStore(db).Get(SettingsStore.HotelName));
    }

    [Fact]
    public void Set_null_removes()
    {
        using var db = TestDb.Create();
        var s = new SettingsStore(db);
        s.Set(SettingsStore.BackupFolder, @"D:\Backups");
        s.Set(SettingsStore.BackupFolder, null);
        Assert.Null(s.Get(SettingsStore.BackupFolder));
    }

    [Fact]
    public void Typed_getters_use_fallbacks()
    {
        using var db = TestDb.Create();
        var s = new SettingsStore(db);
        Assert.False(s.GetBool(SettingsStore.BadgeEnabled));
        Assert.Equal(50, s.GetInt(SettingsStore.SlotCount, 50));
        s.Set(SettingsStore.BadgeEnabled, "1");
        s.Set(SettingsStore.SlotCount, "20");
        Assert.True(s.GetBool(SettingsStore.BadgeEnabled));
        Assert.Equal(20, s.GetInt(SettingsStore.SlotCount, 50));
    }
}
