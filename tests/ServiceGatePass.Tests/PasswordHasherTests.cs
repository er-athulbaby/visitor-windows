using ServiceGatePass.Services;

namespace ServiceGatePass.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_then_verify_round_trips()
    {
        var stored = PasswordHasher.Hash("gate-pass-1");
        Assert.True(PasswordHasher.Verify("gate-pass-1", stored));
    }

    [Fact]
    public void Uses_600k_iterations()
    {
        var salt = new byte[16];
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2("pw", salt, 600_000,
            System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
        Assert.True(PasswordHasher.Verify("pw", $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}"));
    }

    [Fact]
    public void Wrong_password_fails() =>
        Assert.False(PasswordHasher.Verify("wrong", PasswordHasher.Hash("gate-pass-1")));

    [Fact]
    public void Same_password_hashes_differently() =>
        Assert.NotEqual(PasswordHasher.Hash("same"), PasswordHasher.Hash("same"));

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("abc.def")]
    public void Garbage_hash_fails(string stored) =>
        Assert.False(PasswordHasher.Verify("x", stored));
}
