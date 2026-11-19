namespace ServiceGatePass.Data;

public class Visitor
{
    public int Id { get; set; }
    public string Cpr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string? NameAr { get; set; }
    public string? Employer { get; set; }
    public string? LastCompany { get; set; }
    public int? LastLocationId { get; set; }
    public string? LastNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Visit> Visits { get; set; } = [];
}

public class Visit
{
    public int Id { get; set; }
    public int VisitorId { get; set; }
    public Visitor Visitor { get; set; } = null!;
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public string? Company { get; set; }
    public string? Note { get; set; }
    public int? BadgeNo { get; set; }
    public int? SlotNo { get; set; }
    public DateTime CheckInAt { get; set; }
    public int CheckInByUserId { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public int? CheckOutByUserId { get; set; }
}

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? NameAr { get; set; }
    public string Display => L.IsArabic && !string.IsNullOrWhiteSpace(NameAr) ? NameAr! : Name;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum Role { Gate = 0, Admin = 1 }

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Setting
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}
