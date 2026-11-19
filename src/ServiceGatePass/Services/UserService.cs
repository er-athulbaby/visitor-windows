using ServiceGatePass.Data;

namespace ServiceGatePass.Services;

public class UserService(GateDb db)
{
    public const int MinPasswordLength = 6;

    public bool AnyUsers() => db.Users.Any();

    public List<User> All() => db.Users.OrderBy(u => u.Username).ToList();

    public User Create(string username, string displayName, string password, Role role)
    {
        var name = Normalize(username);
        if (name == "") throw new GateException(L.T("Username is required."));
        if (string.IsNullOrWhiteSpace(displayName)) throw new GateException(L.T("Display name is required."));
        CheckPassword(password);
        if (db.Users.Any(u => u.Username == name)) throw new GateException(L.T("That username is already taken."));

        var user = new User { Username = name, DisplayName = displayName.Trim(), PasswordHash = PasswordHasher.Hash(password), Role = role };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public User? Authenticate(string username, string password)
    {
        var name = Normalize(username);
        var user = db.Users.SingleOrDefault(u => u.Username == name);
        return user is { IsActive: true } && PasswordHasher.Verify(password, user.PasswordHash) ? user : null;
    }

    public void ResetPassword(int userId, string password)
    {
        CheckPassword(password);
        Get(userId).PasswordHash = PasswordHasher.Hash(password);
        db.SaveChanges();
    }

    public void SetActive(int userId, bool active)
    {
        var user = Get(userId);
        if (!active) GuardLastAdmin(user);
        user.IsActive = active;
        db.SaveChanges();
    }

    public void SetRole(int userId, Role role)
    {
        var user = Get(userId);
        if (role != Role.Admin) GuardLastAdmin(user);
        user.Role = role;
        db.SaveChanges();
    }

    void GuardLastAdmin(User user)
    {
        if (user.Role == Role.Admin && user.IsActive && db.Users.Count(u => u.Role == Role.Admin && u.IsActive) == 1)
            throw new GateException(L.T("At least one active admin is required."));
    }

    User Get(int id) => db.Users.Find(id) ?? throw new GateException(L.T("User not found."));

    static string Normalize(string username) => username.Trim().ToLowerInvariant();

    static void CheckPassword(string password)
    {
        if (password.Length < MinPasswordLength)
            throw new GateException(string.Format(L.T("Password must be at least {0} characters."), MinPasswordLength));
    }
}
