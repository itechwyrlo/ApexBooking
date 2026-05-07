using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.GenericRepository.Abstractions;
using Microsoft.AspNetCore.Identity;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> FindByEmailAsync(TenantId tenantId, string email);
    Task<User?> FindByEmailAcrossAllTenantsAsync(string email);
    Task<User?> FindByRefreshTokenAsync(string refreshToken);
    Task<bool> EmailExistsAsync(TenantId tenantId, string email);
    Task<User?> FindByInvitationTokenAsync(string token);
    Task<List<User>> GetUsersByRoleAsync(TenantId tenantId, UserRole role);
    Task<List<User>> GetStaffWithResourceAssignmentsAsync(TenantId tenantId, ResourceId resourceId);
    Task<bool> CheckPasswordAsync(User user, string password);
    Task<IdentityResult> CreateAsync(User user, string password);
    Task<IdentityResult> AddToRoleAsync(User user, string role);
    Task<string> GenerateUserTokenAsync(User user, string tokenProvider, string purpose);
    Task<string> GenerateEmailConfirmationTokenAsync(User user);
    
    Task<IdentityResult> ConfirmEmailAsync(User user, string token);
    Task<bool> VerifyUserTokenAsync(User user, string token, string tokenProvider, string purpose);
    Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword);
    Task<User> GetUserByEmailTokenAsync(string token);
}
