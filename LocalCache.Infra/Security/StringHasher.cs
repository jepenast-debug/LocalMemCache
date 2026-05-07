using System.Security.Cryptography;
using System.Text;

namespace LocalCache.Infrastructure.Security;

public static class StringHasher {
    public static string Hash (string StrData) {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(StrData));
        return Convert.ToBase64String(bytes);
    }

    public static bool Verify (string StrData, string HashData) {
        return Hash(StrData) == HashData;
    }
}