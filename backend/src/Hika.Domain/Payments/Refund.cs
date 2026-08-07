using Hika.Domain.Common;

namespace Hika.Domain.Payments;

public enum RefundStatus
{
    Pending,
    Succeeded,
    Failed,
}

public sealed class Refund : AuditableEntity
{
    public Guid PaymentId { get; private set; }

    public Money Amount { get; private set; }

    public string Reason { get; private set; }

    public RefundStatus Status { get; private set; }

    private Refund()
    {
        Amount = Money.Zero();
        Reason = string.Empty;
    }

    public static Refund Request(Guid paymentId, Money amount, string reason) => new()
    {
        PaymentId = paymentId,
        Amount = amount,
        Reason = reason,
        Status = RefundStatus.Pending,
    };

    public void MarkSucceeded()
    {
        if (Status != RefundStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark a {Status} refund as succeeded.");
        }

        Status = RefundStatus.Succeeded;
    }

    public void MarkFailed()
    {
        if (Status != RefundStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark a {Status} refund as failed.");
        }

        Status = RefundStatus.Failed;
    }
}
