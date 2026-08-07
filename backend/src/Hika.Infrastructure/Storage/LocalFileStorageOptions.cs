namespace Hika.Infrastructure.Storage;

public sealed class LocalFileStorageOptions
{
    public const string SectionName = "LocalFileStorage";

    /// <summary>Absolute or working-directory-relative path where uploaded files are written.</summary>
    public required string RootPath { get; init; }

    /// <summary>
    /// Fallback public base URL, used only when no HttpContext is available (e.g. outside a
    /// request). Normally the actual request's scheme+host is used instead — see
    /// LocalFileStorage — so returned URLs are correct whether the caller is an Android
    /// emulator (10.0.2.2), a physical device, or a browser on localhost.
    /// </summary>
    public required string PublicBaseUrl { get; init; }
}
