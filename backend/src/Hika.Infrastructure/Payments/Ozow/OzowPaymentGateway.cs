using System.Globalization;
using System.Net.Http.Json;
using Hika.Application.Payments.Ports;
using Hika.Domain.Common;
using Hika.Domain.Payments;
using Hika.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hika.Infrastructure.Payments.Ozow;

/// <summary>
/// Real Ozow integration (see docs/south-africa.md) — a redirect-based Instant EFT gateway, so
/// unlike MockPaymentGateway neither payments nor refunds settle in the same call; both come
/// back as IsPending results that only resolve later via OzowWebhooksController.
///
/// <b>Field order/endpoint-path caveat:</b> Ozow's HashCheck algorithm (SHA-512 over a fixed
/// concatenation of field values, see OzowHashHelper) is well-documented, but the *exact* field
/// order and the refund endpoint's path/field names could not be confirmed against Ozow's live
/// merchant-portal docs in this environment — they're reconstructed from third-party reference
/// implementations and Ozow's public integration articles. Before relying on this against a
/// real merchant account: register at ozow.com, compare this file's field order against your
/// account's own integration guide (Merchant Admin → Integration), and adjust OzowHashHelper's
/// call sites if they differ. A mismatched hash fails closed (Ozow/our own verification both
/// reject the request) rather than silently misprocessing a payment either way.
/// </summary>
public sealed class OzowPaymentGateway(
    HttpClient httpClient,
    IOptions<OzowOptions> ozowOptions,
    IOptions<LocalFileStorageOptions> fileStorageOptions,
    IHttpContextAccessor httpContextAccessor,
    ILogger<OzowPaymentGateway> logger) : IPaymentGateway
{
    public PaymentProvider Provider => PaymentProvider.Ozow;

    public async Task<PaymentInitiationResult> InitiatePaymentAsync(Guid bookingId, Money amount, CancellationToken cancellationToken)
    {
        var settings = ozowOptions.Value;
        var baseUrl = ResolvePublicBaseUrl();
        var amountString = FormatAmount(amount);
        var transactionReference = bookingId.ToString();
        var bankReference = BuildBankReference(bookingId);
        var cancelUrl = $"{baseUrl}/payment-return/cancel";
        var errorUrl = $"{baseUrl}/payment-return/error";
        var successUrl = $"{baseUrl}/payment-return/success";
        var notifyUrl = $"{baseUrl}/api/v1/webhooks/ozow/payment-notify";

        var hash = OzowHashHelper.ComputeHash(
            [
                settings.SiteCode, "ZA", "ZAR", amountString, transactionReference, bankReference,
                cancelUrl, errorUrl, successUrl, notifyUrl, settings.IsTest.ToString(),
            ],
            settings.PrivateKey);

        var request = new OzowPaymentRequest
        {
            SiteCode = settings.SiteCode,
            Amount = amountString,
            TransactionReference = transactionReference,
            BankReference = bankReference,
            CancelUrl = cancelUrl,
            ErrorUrl = errorUrl,
            SuccessUrl = successUrl,
            NotifyUrl = notifyUrl,
            IsTest = settings.IsTest,
            HashCheck = hash,
        };

        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/postpaymentrequest") { Content = JsonContent.Create(request) };
            httpRequest.Headers.Add("ApiKey", settings.ApiKey);
            response = await httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Ozow PostPaymentRequest failed for booking {BookingId}", bookingId);
            return PaymentInitiationResult.SettledImmediately(succeeded: false, providerReference: null, failureReason: "Could not reach Ozow.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Ozow PostPaymentRequest returned {StatusCode} for booking {BookingId}: {Body}",
                response.StatusCode, bookingId, errorBody);
            return PaymentInitiationResult.SettledImmediately(succeeded: false, providerReference: null, failureReason: "Ozow rejected the payment request.");
        }

        var payload = await response.Content.ReadFromJsonAsync<OzowPaymentResponse>(cancellationToken);
        if (string.IsNullOrWhiteSpace(payload?.Url))
        {
            logger.LogError("Ozow PostPaymentRequest returned no payment URL for booking {BookingId}: {Error}", bookingId, payload?.ErrorMessage);
            return PaymentInitiationResult.SettledImmediately(
                succeeded: false, providerReference: null, failureReason: payload?.ErrorMessage ?? "Ozow did not return a payment URL.");
        }

        return PaymentInitiationResult.Pending(payload.Url, transactionReference);
    }

    public async Task<RefundInitiationResult> InitiateRefundAsync(
        Guid refundId, string originalProviderReference, Money amount, CancellationToken cancellationToken)
    {
        var settings = ozowOptions.Value;
        var baseUrl = ResolvePublicBaseUrl();
        var amountString = FormatAmount(amount);
        var refundReference = refundId.ToString();
        var notifyUrl = $"{baseUrl}/api/v1/webhooks/ozow/refund-notify";

        var hash = OzowHashHelper.ComputeHash(
            [settings.SiteCode, originalProviderReference, refundReference, amountString, notifyUrl, settings.IsTest.ToString()],
            settings.PrivateKey);

        var request = new OzowRefundRequest
        {
            SiteCode = settings.SiteCode,
            TransactionId = originalProviderReference,
            RefundReference = refundReference,
            Amount = amountString,
            NotifyUrl = notifyUrl,
            IsTest = settings.IsTest,
            HashCheck = hash,
        };

        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/postrefund") { Content = JsonContent.Create(request) };
            httpRequest.Headers.Add("ApiKey", settings.ApiKey);
            response = await httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Ozow refund request failed for refund {RefundId} (payment {ProviderReference})", refundId, originalProviderReference);
            return RefundInitiationResult.SettledImmediately(succeeded: false, failureReason: "Could not reach Ozow.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Ozow refund request returned {StatusCode} for refund {RefundId}: {Body}", response.StatusCode, refundId, errorBody);
            return RefundInitiationResult.SettledImmediately(succeeded: false, failureReason: "Ozow rejected the refund request.");
        }

        var payload = await response.Content.ReadFromJsonAsync<OzowRefundResponse>(cancellationToken);
        if (payload?.IsError == true)
        {
            logger.LogError("Ozow refund request was rejected for refund {RefundId}: {Error}", refundId, payload.ErrorMessage);
            return RefundInitiationResult.SettledImmediately(succeeded: false, failureReason: payload.ErrorMessage ?? "Ozow rejected the refund.");
        }

        return RefundInitiationResult.Pending(refundReference);
    }

    private static string FormatAmount(Money amount) => amount.Amount.ToString("F2", CultureInfo.InvariantCulture);

    /// <summary>"HIKA" + the first 8 hex chars of the booking id — short enough to fit a bank
    /// statement reference field and stable/derivable without an extra stored column.</summary>
    private static string BuildBankReference(Guid bookingId) => $"HIKA{bookingId:N}"[..12].ToUpperInvariant();

    /// <summary>Same pattern as LocalFileStorage.ResolveBaseUrl — prefers the current request's
    /// own scheme+host (correct for whichever client actually called us) over the configured
    /// fallback, which would only be right for one deployment target at a time.</summary>
    private string ResolvePublicBaseUrl()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return fileStorageOptions.Value.PublicBaseUrl.TrimEnd('/');
        }

        return $"{request.Scheme}://{request.Host}".TrimEnd('/');
    }
}
