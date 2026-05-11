using System.Security.Cryptography;
using System.Text;

namespace TesteAPI.Services;

public static class PasswordHasher
{
    public static string Hash(string password)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

    public static bool Verify(string password, string stored)
        => Hash(password) == stored;
}
