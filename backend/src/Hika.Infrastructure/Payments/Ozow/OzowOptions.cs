namespace Hika.Infrastructure.Payments.Ozow;

/// <summary>
/// Empty SiteCode/PrivateKey/ApiKey by default and deliberately not validated on start —
/// DependencyInjection only registers OzowPaymentGateway when SiteCode is actually set (see
/// AddInfrastructure), so a deployment without real Ozow credentials keeps using
/// MockPaymentGateway instead of failing to start. Fill these in from Ozow's merchant admin
/// site (Settings → Merchant Details) once you have an account.
/// </summary>
public sealed class OzowOptions
{
    public const string SectionName = "Ozow";

    public string SiteCode { get; init; } = "";

    /// <summary>Never sent to Ozow — only ever hashed together with request/response fields to
    /// produce/verify a HashCheck. Keep this out of logs and error messages.</summary>
    public string PrivateKey { get; init; } = "";

    public string ApiKey { get; init; } = "";

    /// <summary>Ozow's own test-mode flag, sent with every request — distinct from, and in
    /// addition to, using the staging API host below. Defaults to true so a half-configured
    /// deployment fails safe (test transactions, not real money) rather than the reverse.</summary>
    public bool IsTest { get; init; } = true;

    public string ApiBaseUrl { get; init; } = "https://stagingapi.ozow.com";
}
