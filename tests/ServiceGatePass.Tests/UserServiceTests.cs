using ServiceGatePass.Data;
using ServiceGatePass.Services;

namespace ServiceGatePass.Tests;

public class UserServiceTests
{
    [Fact]
    public void Create_and_authenticate_ignores_username_case_and_spaces()
    {
        using var db = TestDb.Create();
        var users = new UserService(db);
        users.Create(" Guard1 ", "Guard One", "secret1", Role.Gate);

        var user = users.Authenticate("GUARD1", "secret1");

        Assert.NotNull(user);
        Assert.Equal("guard1", user!.Username);
        Assert.Null(users.Authenticate("guard1", "wrong"));
        Assert.True(users.AnyUsers());
    }

    [Fact]
    public void Disabled_user_cannot_sign_in()
    {
        using var db = TestDb.Create();
        var users = new UserService(db);
        users.Create("admin", "Admin", "secret1", Role.Admin);
        var guard = users.Create("guard", "Guard", "secret1", Role.Gate);

        users.SetActive(guard.Id, false);

        Assert.Null(users.Authenticate("guard", "secret1"));
    }

    [Fact]
    public void Duplicate_username_is_refused()
    {
        using var db = TestDb.Create();
        var users = new UserService(db);
        users.Create("guard", "Guard", "secret1", Role.Gate);
        var ex = Assert.Throws<GateException>(() => users.Create("GUARD", "Other", "secret1", Role.Gate));
        Assert.Equal("That username is already taken.", ex.Message);
    }

    [Fact]
    public void Short_password_is_refused()
    {
        using var db = TestDb.Create();
        var ex = Assert.Throws<GateException>(() => new UserService(db).Create("guard", "Guard", "12345", Role.Gate));
        Assert.Equal("Password must be at least 6 characters.", ex.Message);
    }

    [Fact]
    public void Reset_password_changes_it()
    {
        using var db = TestDb.Create();
        var users = new UserService(db);
        var guard = users.Create("guard", "Guard", "secret1", Role.Gate);
        users.ResetPassword(guard.Id, "secret2");
        Assert.Null(users.Authenticate("guard", "secret1"));
        Assert.NotNull(users.Authenticate("guard", "secret2"));
    }

    [Fact]
    public void Last_active_admin_cannot_be_disabled_or_demoted()
    {
        using var db = TestDb.Create();
        var users = new UserService(db);
        var admin = users.Create("admin", "Admin", "secret1", Role.Admin);

        Assert.Equal("At least one active admin is required.",
            Assert.Throws<GateException>(() => users.SetActive(admin.Id, false)).Message);
        Assert.Throws<GateException>(() => users.SetRole(admin.Id, Role.Gate));

        var second = users.Create("admin2", "Admin Two", "secret1", Role.Admin);
        users.SetActive(admin.Id, false);
        Assert.Null(users.Authenticate("admin", "secret1"));
        Assert.NotNull(users.Authenticate("admin2", "secret1"));
        Assert.Equal(second.Id, users.All().Single(u => u.IsActive).Id);
    }
}
