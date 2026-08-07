namespace Hika.Application.Search.Dtos;

/// <summary>Autocomplete suggestion — a UX aid only. The client can always submit free text for
/// an unlisted village instead (see Hika.Domain.Trips.Location).</summary>
public sealed record LocationResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Province { get; init; }

    public required string Type { get; init; }
}
