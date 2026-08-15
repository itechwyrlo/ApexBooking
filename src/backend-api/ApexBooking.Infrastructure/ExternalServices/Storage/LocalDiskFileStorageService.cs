using ApexBooking.Core.Domain.Services;
using ApexBooking.Infrastructure.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace ApexBooking.Infrastructure.ExternalServices.Storage
{
    /// <summary>
    /// Writes uploaded content to the API server's own wwwroot, served back out by the existing
    /// app.UseStaticFiles() middleware. Fine for a single persistent instance; swap for a
    /// cloud-backed implementation behind IFileStorageService if the deployment ever needs to
    /// scale to multiple instances (see the profile customization design spec).
    /// </summary>
    public class LocalDiskFileStorageService : IFileStorageService
    {
        private const string RelativeRoot = "uploads/profile-photos";

        private readonly IWebHostEnvironment _env;
        private readonly ApplicationUrlsSettings _urls;

        public LocalDiskFileStorageService(IWebHostEnvironment env, IOptions<ApplicationUrlsSettings> urls)
        {
            _env = env;
            _urls = urls.Value;
        }

        public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            var relativePath = $"{RelativeRoot}/{fileName}";
            var physicalPath = Path.Combine(_env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

            await using (var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            return $"{_urls.BaseUrl.TrimEnd('/')}/{relativePath}";
        }

        public Task DeleteAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                var relativePath = GetRelativePathFromUrl(url);
                if (relativePath is null)
                    return Task.CompletedTask;

                var physicalPath = Path.Combine(_env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(physicalPath))
                    File.Delete(physicalPath);
            }
            catch
            {
                // Best-effort: a delete failure must never fail the request that triggered it
                // (e.g. replacing a photo, or removing a stale file already gone from disk).
            }

            return Task.CompletedTask;
        }

        private static string? GetRelativePathFromUrl(string url)
        {
            var marker = $"/{RelativeRoot}/";
            var index = url.IndexOf(marker, StringComparison.Ordinal);
            return index < 0 ? null : url[(index + 1)..];
        }
    }
}
