using Users.Infrastructure.Security;

namespace Users.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Verify_ReturnsTrue_ForSamePassword()
    {
        var hash = _hasher.Hash("secret");

        Assert.True(_hasher.Verify("secret", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForDifferentPassword()
    {
        var hash = _hasher.Hash("secret");

        Assert.False(_hasher.Verify("Secret", hash));
    }
}
