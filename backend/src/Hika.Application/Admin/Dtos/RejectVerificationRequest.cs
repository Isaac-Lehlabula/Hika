namespace Hika.Application.Admin.Dtos;

public sealed record RejectVerificationRequest
{
    public required string Reason { get; init; }
}
