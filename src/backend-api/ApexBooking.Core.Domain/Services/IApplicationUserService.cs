using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Services
{
    public interface IApplicationUserService
    {
        Task<bool> ValidateUserByEmailAsync(string email);
        Task<UserCreatedResponse> CreatedUserAsync(string email, string firstName, string LastName);

        Task<PasswordSetupTicket?> GeneratePasswordResetTokenAsync(string email);
        Task<ApplicationUserSummary?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<UserResponse?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Non-throwing lookup — unlike GetUserByEmailAsync, returns null for "not found" instead of
        /// throwing. For flows like "find my workspace" where a missing account is an expected,
        /// silent outcome (never disclosed as a distinct error), not an authorization failure.
        /// </summary>
        Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<PasswordResetResult> ResetPasswordAsync(
            Guid userId,
            string resetToken,
            string newPassword,
            TenantId? tenantId,
            SystemRole? role,
            string? slug,
            TenantMemberId? tenantMemberId,
            CancellationToken cancellationToken = default);

        Task LoginAsync(string email, string password);
        Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mints a new refresh token secret for an already-authenticated user (post password check)
        /// and persists its hash. Returns the raw secret so the caller can set it as the HttpOnly cookie.
        /// </summary>
        Task<string> IssueRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consumes the presented refresh token and issues its replacement. The caller passes the
        /// raw secret straight from the cookie — only its hash is ever compared or stored.
        /// Returns <c>null</c> when the secret is unknown, already consumed, revoked, expired, or
        /// belongs to a deactivated account, so the caller can respond with a single
        /// indistinguishable failure.
        /// </summary>
        Task<RefreshTokenRotation?> RotateRefreshTokenAsync(
            string presentedRefreshTokenSecret,
            CancellationToken cancellationToken = default);

        /// <summary>All platform admin (ApplicationUser.IsPlatformAdmin) user ids — used to fan out
        /// platform-wide notifications (e.g. trial-expiry events) to every SuperAdmin.</summary>
        Task<IReadOnlyList<Guid>> GetPlatformAdminIdsAsync(CancellationToken cancellationToken = default);

        Task<ApplicationUserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

        Task UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phoneNumber, CancellationToken cancellationToken = default);

        Task UpdatePhotoAsync(Guid userId, string? photoUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies <paramref name="currentPassword"/> and sets <paramref name="newPassword"/>,
        /// then revokes every refresh token for this user — including the one behind the request
        /// making this call. No new tokens are issued; the caller is expected to log in again.
        /// </summary>
        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    }

    public class UserResponse
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsPlatformAdmin {get; set;}
    }
    public class UserCreatedResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsSucceeded { get; set; }
        public IEnumerable<string>? Errors { get; set; }
    }

    public sealed record PasswordSetupTicket(Guid UserId, string Token);

    public sealed record ApplicationUserSummary(
        Guid UserId,
        string Email,
        string FullName,
        bool IsPlatformAdmin,
        bool IsActive);

    public sealed record ApplicationUserProfile(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        string FullName,
        string? PhoneNumber,
        string? PhotoUrl,
        bool IsPlatformAdmin);

    /// <summary>
    /// Outcome of a successful refresh-token rotation: enough identity to mint the next access
    /// token, plus the new raw secret — the only moment it exists in plaintext, so it must go
    /// straight into the HttpOnly cookie and never into a response body or a log.
    /// </summary>
    public sealed record RefreshTokenRotation(
        Guid UserId,
        string Email,
        string FullName,
        bool IsPlatformAdmin,
        string RefreshTokenSecret);

    public sealed record PasswordResetResult(
        Guid UserId,
        string Email,
        string FullName,
        string AccessToken,
        string RefreshToken);
}
