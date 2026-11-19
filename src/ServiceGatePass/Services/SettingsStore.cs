using ServiceGatePass.Data;

namespace ServiceGatePass.Services;

public class SettingsStore(GateDb db)
{
    public const string HotelName = "HotelName";
    public const string LogoPath = "LogoPath";
    public const string BadgeEnabled = "BadgeEnabled";
    public const string SlotEnabled = "SlotEnabled";
    public const string SlotCount = "SlotCount";
    public const string BackupFolder = "BackupFolder";
    public const string ReaderUrl = "ReaderUrl";
    public const string Language = "Language";

    public string? Get(string key) => db.Settings.Find(key)?.Value;

    public void Set(string key, string? value)
    {
        var row = db.Settings.Find(key);
        if (value == null)
        {
            if (row != null) db.Settings.Remove(row);
        }
        else if (row == null) db.Settings.Add(new Setting { Key = key, Value = value });
        else row.Value = value;
        db.SaveChanges();
    }

    public bool GetBool(string key) => Get(key) == "1";

    public int GetInt(string key, int fallback) => int.TryParse(Get(key), out var v) ? v : fallback;
}
