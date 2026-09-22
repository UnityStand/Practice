using System.Security.Cryptography;
using System.Text;
using Users.Application.Abstractions;

namespace Users.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string password, string hash)
    {
        return Hash(password) == hash;
    }
}