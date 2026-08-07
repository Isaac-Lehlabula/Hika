namespace Hika.Application.Payments.Dtos;

public sealed record RefundRequest
{
    public required string Reason { get; init; }
}
