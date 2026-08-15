namespace ApexBooking.Core.Domain.Services
{
    /// <summary>
    /// Stores arbitrary binary content (currently: profile photos) and returns a URL the browser
    /// can load directly. <paramref name="fileName"/> may include subdirectories (e.g.
    /// "{userId}/{guid}.jpg") — it is the full relative path under the storage root, not just a
    /// leaf name. See LocalDiskFileStorageService (Infrastructure) for the current implementation
    /// — local disk under wwwroot, chosen for a single-instance deployment (see the profile
    /// customization design spec); swap in a cloud-backed implementation behind this interface if
    /// the deployment ever needs to scale to multiple instances.
    /// </summary>
    public interface IFileStorageService
    {
        Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);

        /// <summary>Best-effort delete — callers must not let a missing/already-deleted file fail
        /// the request that triggered the delete.</summary>
        Task DeleteAsync(string url, CancellationToken cancellationToken = default);
    }
}
