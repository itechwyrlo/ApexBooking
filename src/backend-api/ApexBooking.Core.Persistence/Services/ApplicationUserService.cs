using System.Security.Cryptography;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Auth;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.Core.Persistence.Identity;
using ApexBooking.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Services
{
    public class ApplicationUserService : IApplicationUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ApexBookingDbContext _context;

        public ApplicationUserService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            ApexBookingDbContext context)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
        }

        public async Task<bool> ValidateUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user is not null;
        }

        public async Task<UserCreatedResponse> CreatedUserAsync(string email, string firstName, string lastName)
        {
            var newUser = ApplicationUser.Create(email, firstName, lastName);
            var randomPass = GenerateTemporaryPassword(16);
            var result = await _userManager.CreateAsync(newUser, randomPass);
            var response = new UserCreatedResponse
            {
                Id = newUser.Id,
                Email = newUser.Email,
                IsSucceeded = result.Succeeded,
                Errors = result.Succeeded ? null : result.Errors.Select(e => e.Description).ToList()
            };

            return response;
        }

        public async Task<PasswordSetupTicket?> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) return null;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            return new PasswordSetupTicket(user.Id, token);
        }

        public async Task<ApplicationUserSummary?> GetUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null) return null;

            return new ApplicationUserSummary(
                user.Id,
                user.Email!,
                user.FullName,
                user.IsPlatformAdmin,
                user.IsActive);
        }

        public async Task<UserResponse?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if(user is null) throw new UnauthorizedException("Login in failed, the user doesn't exist");
            var response = new UserResponse()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                IsPlatformAdmin = user.IsPlatformAdmin
            };

            return response;
        }

        public async Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user?.Id;
        }

        public async Task<PasswordResetResult> ResetPasswordAsync(
            Guid userId,
            string resetToken,
            string newPassword,
            TenantId? tenantId,
            SystemRole? role,
            string? slug,
            TenantMemberId? tenantMemberId,
            CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;

            var user = await _userManager.Users
                .Include(x => x.RefreshTokens)
                .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user is null)
                throw new BusinessRuleBrokenException("Failed to reset password, User doesn't exist");

            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
                throw new BusinessRuleBrokenException(
                    string.Join(" ", result.Errors.Select(e => e.Description)));

            user.MarkAsActive(utcNow);
            user.MarkEmailAsConfirmed(utcNow);

            user.RevokeAllRefreshTokens(utcNow);

            var accessToken = _tokenService.GenerateAccessToken(
                new TokenDescriptor(
                    user.Id,
                    user.Email!,
                    user.FullName,
                    user.IsPlatformAdmin,
                    tenantId,
                    role,
                    slug,
                    tenantMemberId));

            var refreshTokenRaw = GenerateRefreshTokenRaw();
            var newRefreshToken = user.AddRefreshToken(HashRefreshToken(refreshTokenRaw), utcNow);
            user.RecordSuccessfulLogin(utcNow);

            // UserManager.UpdateAsync() calls Context.Update(user) internally, which walks the
            // whole navigation graph and marks any untracked entity with a non-default key as
            // Modified rather than Added — RefreshToken generates its own Guid Id in its
            // constructor, so without this it gets saved as an UPDATE that matches zero rows,
            // surfacing as a misleading "optimistic concurrency failure".
            _context.Add(newRefreshToken);

            await _userManager.UpdateAsync(user);

            return new PasswordResetResult(
                user.Id,
                user.Email!,
                user.FullName,
                accessToken,
                refreshTokenRaw);
        }

        public async Task LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) throw new UnauthorizedException("Failed to Login, Invalid email or password");
            var isValidPassword = await _userManager.CheckPasswordAsync(user, password);
            if (!isValidPassword) throw new UnauthorizedException("Failed to Login, Invalid email or password");
        }

        public async Task<string> IssueRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;

            var user = await _userManager.Users
                .Include(x => x.RefreshTokens)
                .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user is null)
                throw new BusinessRuleBrokenException("Failed to issue refresh token, user doesn't exist");

            var refreshTokenRaw = GenerateRefreshTokenRaw();
            var newRefreshToken = user.AddRefreshToken(HashRefreshToken(refreshTokenRaw), utcNow);
            user.RecordSuccessfulLogin(utcNow);

            // See the ResetPasswordAsync comment above: without this, UserManager.UpdateAsync's
            // internal Context.Update(user) mis-tracks the new RefreshToken as Modified (its Id
            // is a client-generated, non-default Guid) and the insert is sent as a no-op UPDATE.
            _context.Add(newRefreshToken);

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new BusinessRuleBrokenException(
                    string.Join(" ", result.Errors.Select(e => e.Description)));

            return refreshTokenRaw;
        }

        public async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;

            var user = await _userManager.Users
                .Include(x => x.RefreshTokens)
                .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user is null)
                return;

            user.RevokeAllRefreshTokens(utcNow);
            await _userManager.UpdateAsync(user);
        }

        public async Task<RefreshTokenRotation?> RotateRefreshTokenAsync(
            string presentedRefreshTokenSecret,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(presentedRefreshTokenSecret))
                return null;

            var utcNow = DateTime.UtcNow;
            var presentedHash = HashRefreshToken(presentedRefreshTokenSecret);

            // Only the hash is ever compared — the raw secret exists in the cookie and nowhere else.
            var user = await _userManager.Users
                .Include(x => x.RefreshTokens)
                .SingleOrDefaultAsync(
                    x => x.RefreshTokens.Any(t => t.RefreshTokenSecret == presentedHash),
                    cancellationToken);

            if (user is null)
                return null;

            var presented = user.RefreshTokens.First(t => t.RefreshTokenSecret == presentedHash);

            if (!presented.CanBeUsed(utcNow))
            {
                // The secret is real but spent, revoked, or expired. A replayed secret means the
                // cookie leaked, so the whole family dies rather than just this one link.
                user.RevokeAllRefreshTokens(utcNow);
                await _userManager.UpdateAsync(user);
                return null;
            }

            if (!user.IsActive)
            {
                user.RevokeAllRefreshTokens(utcNow);
                await _userManager.UpdateAsync(user);
                return null;
            }

            var newRefreshTokenRaw = GenerateRefreshTokenRaw();

            // Rotate marks the presented token used + revoked and appends its replacement,
            // so a stolen copy of the old secret is dead the moment this commits.
            var rotatedRefreshToken = user.RotateRefreshToken(presentedHash, HashRefreshToken(newRefreshTokenRaw), utcNow);

            // See ResetPasswordAsync: the replacement token's client-generated Guid Id would
            // otherwise be mis-tracked as Modified by UpdateAsync's internal Context.Update(user).
            _context.Add(rotatedRefreshToken);

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new BusinessRuleBrokenException(
                    string.Join(" ", result.Errors.Select(e => e.Description)));

            return new RefreshTokenRotation(
                user.Id,
                user.Email!,
                user.FullName,
                user.IsPlatformAdmin,
                newRefreshTokenRaw);
        }

        public async Task<IReadOnlyList<Guid>> GetPlatformAdminIdsAsync(CancellationToken cancellationToken = default)
        {
            return await _userManager.Users
                .Where(u => u.IsPlatformAdmin)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<ApplicationUserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null) return null;

            return new ApplicationUserProfile(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.PhoneNumber,
                user.PhotoUrl,
                user.IsPlatformAdmin);
        }

        public async Task UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phoneNumber, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new BusinessRuleBrokenException("Failed to update profile, user doesn't exist");

            user.UpdateProfile(firstName, lastName, phoneNumber, DateTime.UtcNow);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BusinessRuleBrokenException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        public async Task UpdatePhotoAsync(Guid userId, string? photoUrl, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new BusinessRuleBrokenException("Failed to update photo, user doesn't exist");

            user.UpdatePhoto(photoUrl, DateTime.UtcNow);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BusinessRuleBrokenException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;

            var user = await _userManager.Users
                .Include(x => x.RefreshTokens)
                .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user is null)
                throw new BusinessRuleBrokenException("Failed to change password, user doesn't exist");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
                throw new BusinessRuleBrokenException(string.Join(" ", result.Errors.Select(e => e.Description)));

            user.RevokeAllRefreshTokens(utcNow);
            await _userManager.UpdateAsync(user);
        }

        // This temporary password is never shown to or used by anyone — it's immediately
        // superseded by the invite/reset-password flow — but it still has to satisfy Identity's
        // password policy for CreateAsync to succeed. Picking purely at random from the combined
        // pool (the previous implementation) had a real chance of missing a required category
        // (e.g. ~11% chance of no digit in 16 draws), causing CreateAsync to intermittently fail.
        private static string GenerateTemporaryPassword(int length = 16)
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string symbols = "!@#$%^&*()_-+=<>?";
            const string allChars = lower + upper + digits + symbols;

            var chars = new List<char>
            {
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                symbols[RandomNumberGenerator.GetInt32(symbols.Length)],
            };

            for (int i = chars.Count; i < length; i++)
                chars.Add(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);

            // Fisher-Yates shuffle so the guaranteed categories aren't always in the first 4 slots.
            for (int i = chars.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }

        private static string GenerateRefreshTokenRaw()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        private static string HashRefreshToken(string refreshToken)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(refreshToken);
            return Convert.ToBase64String(SHA256.HashData(bytes));
        }
    }
}
