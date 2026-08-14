using System.Text.Json.Serialization;

namespace Hika.Infrastructure.Payments.Ozow;

/// <summary>Body for POST {ApiBaseUrl}/postpaymentrequest.</summary>
internal sealed record OzowPaymentRequest
{
    public required string SiteCode { get; init; }

    public string CountryCode { get; init; } = "ZA";

    public string CurrencyCode { get; init; } = "ZAR";

    public required string Amount { get; init; }

    public required string TransactionReference { get; init; }

    public required string BankReference { get; init; }

    public required string CancelUrl { get; init; }

    public required string ErrorUrl { get; init; }

    public required string SuccessUrl { get; init; }

    public required string NotifyUrl { get; init; }

    public required bool IsTest { get; init; }

    public required string HashCheck { get; init; }
}

internal sealed record OzowPaymentResponse
{
    public string? Url { get; init; }

    public string? RequestId { get; init; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }
}

/// <summary>Body for a refund request — endpoint path and exact field names are extrapolated
/// from the payment-request shape, not confirmed against a live Ozow "Refunds" product account
/// (a separate product/enrollment from base payments — see ozow.com/our-products/refunds).
/// Verify before relying on this in production.</summary>
internal sealed record OzowRefundRequest
{
    public required string SiteCode { get; init; }

    /// <summary>The original payment's Ozow-assigned TransactionId (Payment.ProviderReference)
    /// — refunds are processed against Ozow's own transaction identity, not our BookingId.</summary>
    public required string TransactionId { get; init; }

    public required string RefundReference { get; init; }

    public required string Amount { get; init; }

    public required string NotifyUrl { get; init; }

    public required bool IsTest { get; init; }

    public required string HashCheck { get; init; }
}

internal sealed record OzowRefundResponse
{
    public string? RefundId { get; init; }

    public bool? IsError { get; init; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }
}

/// <summary>Server-to-server payload Ozow posts to NotifyUrl once a payment settles — bound
/// via [FromForm] since Ozow's documented examples show classic form-POST variables, not JSON.
/// See OzowWebhooksController.</summary>
public sealed record OzowPaymentNotifyPayload
{
    public string SiteCode { get; init; } = "";

    public string TransactionId { get; init; } = "";

    public string TransactionReference { get; init; } = "";

    public string Amount { get; init; } = "";

    public string Status { get; init; } = "";

    public string CurrencyCode { get; init; } = "";

    public string IsTest { get; init; } = "";

    public string StatusMessage { get; init; } = "";

    public string Hash { get; init; } = "";
}

/// <summary>Notify payload for the separate Refunds product — same caveat as
/// OzowRefundRequest.</summary>
public sealed record OzowRefundNotifyPayload
{
    public string SiteCode { get; init; } = "";

    public string RefundId { get; init; } = "";

    public string RefundReference { get; init; } = "";

    public string Amount { get; init; } = "";

    public string Status { get; init; } = "";

    public string IsTest { get; init; } = "";

    public string Hash { get; init; } = "";
}
