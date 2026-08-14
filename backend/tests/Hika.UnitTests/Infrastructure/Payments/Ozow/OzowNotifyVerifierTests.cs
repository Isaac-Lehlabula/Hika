using Hika.Infrastructure.Payments.Ozow;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Hika.UnitTests.Infrastructure.Payments.Ozow;

public class OzowNotifyVerifierTests
{
    private const string PrivateKey = "test-private-key";
    private readonly OzowNotifyVerifier _sut = new(Options.Create(new OzowOptions { PrivateKey = PrivateKey }));

    private static OzowPaymentNotifyPayload BuildPaymentPayload() => new()
    {
        SiteCode = "TESTSITE",
        TransactionId = "OZOW-TXN-1",
        TransactionReference = Guid.NewGuid().ToString(),
        Amount = "300.00",
        Status = "Complete",
        CurrencyCode = "ZAR",
        IsTest = "True",
    };

    private static string ComputePaymentHash(OzowPaymentNotifyPayload payload, string privateKey) =>
        OzowHashHelper.ComputeHash(
            [payload.SiteCode, payload.TransactionId, payload.TransactionReference, payload.Amount, payload.Status, payload.CurrencyCode, payload.IsTest],
            privateKey);

    [Fact]
    public void VerifyPaymentNotify_CorrectHash_ReturnsTrue()
    {
        var payload = BuildPaymentPayload();
        payload = payload with { Hash = ComputePaymentHash(payload, PrivateKey) };

        _sut.VerifyPaymentNotify(payload).ShouldBeTrue();
    }

    [Fact]
    public void VerifyPaymentNotify_TamperedAmount_ReturnsFalse()
    {
        var payload = BuildPaymentPayload();
        var hash = ComputePaymentHash(payload, PrivateKey);
        var tampered = payload with { Hash = hash, Amount = "999.99" };

        _sut.VerifyPaymentNotify(tampered).ShouldBeFalse();
    }

    [Fact]
    public void VerifyPaymentNotify_HashSignedWithWrongPrivateKey_ReturnsFalse()
    {
        var payload = BuildPaymentPayload();
        payload = payload with { Hash = ComputePaymentHash(payload, "a-different-key") };

        _sut.VerifyPaymentNotify(payload).ShouldBeFalse();
    }

    [Fact]
    public void VerifyPaymentNotify_HashIsCaseInsensitive()
    {
        var payload = BuildPaymentPayload();
        payload = payload with { Hash = ComputePaymentHash(payload, PrivateKey).ToUpperInvariant() };

        _sut.VerifyPaymentNotify(payload).ShouldBeTrue();
    }

    private static OzowRefundNotifyPayload BuildRefundPayload() => new()
    {
        SiteCode = "TESTSITE",
        RefundId = "OZOW-REFUND-1",
        RefundReference = Guid.NewGuid().ToString(),
        Amount = "300.00",
        Status = "Complete",
        IsTest = "True",
    };

    private static string ComputeRefundHash(OzowRefundNotifyPayload payload, string privateKey) =>
        OzowHashHelper.ComputeHash(
            [payload.SiteCode, payload.RefundId, payload.RefundReference, payload.Amount, payload.Status, payload.IsTest],
            privateKey);

    [Fact]
    public void VerifyRefundNotify_CorrectHash_ReturnsTrue()
    {
        var payload = BuildRefundPayload();
        payload = payload with { Hash = ComputeRefundHash(payload, PrivateKey) };

        _sut.VerifyRefundNotify(payload).ShouldBeTrue();
    }

    [Fact]
    public void VerifyRefundNotify_TamperedStatus_ReturnsFalse()
    {
        var payload = BuildRefundPayload();
        var hash = ComputeRefundHash(payload, PrivateKey);
        var tampered = payload with { Hash = hash, Status = "Error" };

        _sut.VerifyRefundNotify(tampered).ShouldBeFalse();
    }
}
