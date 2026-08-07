using Hika.Application.Common.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hika.Infrastructure.Storage;

/// <summary>
/// MVP file storage: writes to local disk under the host's content root, served back out via
/// ASP.NET Core static files (see Program.cs, which resolves the same absolute path so the
/// write location and the serve location can never drift apart). Fine for a single-instance
/// MVP; swapping in Azure Blob/S3 later means implementing this one interface again — nothing
/// else in the codebase references the filesystem directly.
/// </summary>
public sealed class LocalFileStorage(
    IOptions<LocalFileStorageOptions> options, IHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
    : IFileStorage
{
    public static string ResolveAbsoluteRootPath(string rootPath, IHostEnvironment environment) =>
        Path.IsPathRooted(rootPath) ? rootPath : Path.Combine(environment.ContentRootPath, rootPath);

    public async Task<string> SaveAsync(
        Stream content, string folder, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var safeFolder = SanitizeSegment(folder);
        var safeFileName = SanitizeSegment(Path.GetFileName(fileName));
        var uniqueFileName = $"{Guid.NewGuid():N}-{safeFileName}";

        var directory = Path.Combine(ResolveAbsoluteRootPath(options.Value.RootPath, environment), safeFolder);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, uniqueFileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return $"{ResolveBaseUrl()}/{safeFolder}/{uniqueFileName}";
    }

    /// <summary>
    /// Uses the current request's own scheme+host when available, so the returned URL is
    /// reachable by whichever client made the request (Android emulator's 10.0.2.2, a
    /// physical device's LAN address, a browser on localhost) — a config-baked URL would only
    /// be correct for one of those at a time.
    /// </summary>
    private string ResolveBaseUrl()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return options.Value.PublicBaseUrl.TrimEnd('/');
        }

        return $"{request.Scheme}://{request.Host}".TrimEnd('/');
    }

    private static string SanitizeSegment(string segment)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(segment.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "file" : cleaned;
    }
}
