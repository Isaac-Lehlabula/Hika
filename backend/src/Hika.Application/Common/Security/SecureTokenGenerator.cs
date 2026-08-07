using System.Security.Cryptography;

namespace Hika.Application.Common.Security;

/// <summary>Cryptographically random values for bearer tokens and OTP codes.</summary>
public static class SecureTokenGenerator
{
    /// <summary>URL-safe opaque token (refresh tokens, email/password reset links).</summary>
    public static string GenerateUrlSafeToken(int byteLength = 32) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    /// <summary>A numeric one-time code, e.g. "483920", suitable for SMS.</summary>
    public static string GenerateNumericOtp(int digits = 6)
    {
        Span<char> chars = stackalloc char[digits];
        for (var i = 0; i < digits; i++)
        {
            chars[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(chars);
    }
}
