namespace Hika.Application.Admin.Dtos;

public sealed record RemoveTripRequest
{
    public required string Reason { get; init; }
}
