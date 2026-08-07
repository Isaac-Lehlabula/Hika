namespace Hika.Application.Common.Options;

/// <summary>Base URL of the frontend, used to build links embedded in verification/reset emails.</summary>
public sealed class NotificationLinksOptions
{
    public const string SectionName = "Frontend";

    public required string BaseUrl { get; init; }
}
