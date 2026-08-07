namespace Hika.Application.Admin.Dtos;

public sealed record AdminRefundRequest
{
    public required string Reason { get; init; }
}
