using System.Security.Cryptography;
using System.Text;

namespace Hika.Infrastructure.Payments.Ozow;

/// <summary>
/// Ozow's HashCheck/Hash algorithm, used both to sign our outgoing PostPaymentRequest/refund
/// requests and to verify incoming notify-webhook payloads: concatenate the relevant fields'
/// values (excluding the hash field itself) in a fixed order, append the merchant's private
/// key, lowercase the whole string, then SHA-512 it. A pure function deliberately kept
/// separate from the HTTP plumbing so the field order — the one part of this integration that
/// couldn't be confirmed against Ozow's live merchant docs in this environment (see
/// OzowPaymentGateway's remarks) — is easy to find and adjust in one place.
/// </summary>
internal static class OzowHashHelper
{
    public static string ComputeHash(IEnumerable<string?> orderedValues, string privateKey)
    {
        var concatenated = string.Concat(orderedValues.Select(v => v ?? string.Empty)) + privateKey;
        var lowercase = concatenated.ToLowerInvariant();
        var hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(lowercase));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
