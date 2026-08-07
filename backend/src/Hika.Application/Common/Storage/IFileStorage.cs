namespace Hika.Application.Common.Storage;

/// <summary>
/// MVP implementation writes to local disk, served via static files (see Infrastructure).
/// Swappable for cloud blob storage (Azure Blob, S3-compatible) later without any
/// Application-layer change — nothing above this interface knows where files actually live.
/// </summary>
public interface IFileStorage
{
    /// <param name="folder">Logical grouping, e.g. "vehicle-photos", "verification-documents".</param>
    /// <returns>A URL the file can be retrieved from.</returns>
    Task<string> SaveAsync(Stream content, string folder, string fileName, string contentType, CancellationToken cancellationToken);
}
