using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    private readonly UserManager<User> _userManager;

    public UserRepository(ApexBookingDbContext context, UserManager<User> userManager) : base(context)
    {
        _userManager = userManager;
    }

    public async Task<User?> FindByEmailAsync(TenantId tenantId, string email)
    {
        return await _userManager.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.UserResourceAssignments)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email);
    }

    public async Task<User?> FindByEmailAcrossAllTenantsAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<User?> FindByRefreshTokenAsync(string refreshToken)
    {
        return await _userManager.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.UserResourceAssignments)
            .IgnoreQueryFilters()
            .Where(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> EmailExistsAsync(TenantId tenantId, string email)
    {
        return await _userManager.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.TenantId == tenantId && u.Email == email);
    }

    public async Task<User?> FindByInvitationTokenAsync(string token)
    {
        return await _userManager.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.UserResourceAssignments)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.InvitationToken == token && u.InvitationExpiresAt > DateTime.UtcNow);
    }

    public async Task<List<User>> GetUsersByRoleAsync(TenantId tenantId, UserRole role)
    {
        return await _userManager.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId && u.Role == role)
            .ToListAsync();
    }

    public async Task<List<User>> GetStaffWithResourceAssignmentsAsync(TenantId tenantId, ResourceId resourceId)
    {
        return await _userManager.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId && u.Role == UserRole.Staff)
            .Where(u => u.UserResourceAssignments.Any(ura => ura.ResourceId == resourceId))
            .ToListAsync();
    }

    public async Task<IdentityResult> CreateUserAsync(User user, string password)
        => await _userManager.CreateAsync(user, password);

    public async Task<IdentityResult> AddToRoleAsync(User user, string role)
        => await _userManager.AddToRoleAsync(user, role);

    public async Task<string> GenerateEmailConfirmationTokenAsync(User user)
        => await _userManager.GenerateEmailConfirmationTokenAsync(user);

    public async Task<IdentityResult> ConfirmEmailAsync(User user, string token)
        => await _userManager.ConfirmEmailAsync(user, token);

    public async Task<bool> CheckPasswordAsync(User user, string password)
        => await _userManager.CheckPasswordAsync(user, password);

    public async Task<IdentityResult> CreateAsync(User user, string password)
        => await _userManager.CreateAsync(user, password);

    public async Task<string> GenerateUserTokenAsync(User user, string tokenProvider, string purpose)
        => await _userManager.GenerateUserTokenAsync(user, tokenProvider, purpose);

    public async Task<bool> VerifyUserTokenAsync(User user, string token, string tokenProvider, string purpose)
        => await _userManager.VerifyUserTokenAsync(user, tokenProvider, purpose, token);

    public async Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword)
        => await _userManager.ResetPasswordAsync(user, token, newPassword);

    public async Task<IList<string>> GetRolesAsync(User user)
        => await _userManager.GetRolesAsync(user);

    public async Task<User> GetUserByEmailTokenAsync(string token)
        => await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.EmailConfirmationToken == token);
}