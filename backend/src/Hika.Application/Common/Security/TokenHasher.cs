using System.Security.Cryptography;
using System.Text;

namespace Hika.Application.Common.Security;

/// <summary>
/// Hashes opaque bearer values (refresh tokens, email-verification/password-reset tokens,
/// phone OTP codes) before they're stored, so a database leak never hands out usable
/// credentials. Deterministic pure function — not a DI abstraction, nothing to swap.
/// </summary>
public static class TokenHasher
{
    public static string Hash(string rawValue)
    {
        var bytes = Encoding.UTF8.GetBytes(rawValue);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
