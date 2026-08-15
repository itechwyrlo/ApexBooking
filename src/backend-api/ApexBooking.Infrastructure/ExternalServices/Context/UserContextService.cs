using System.Security.Claims;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Infrastructure.ExternalServices.Context
{
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid GetCurrentUserId()
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                throw new UnauthorizedException("User is not authenticated.");

            return userId;
        }
        public string GetUserRole()
        {
            var role = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return role ?? string.Empty;
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        // Same claim/check TenantMiddleware already uses to decide whether to bypass tenant
        // resolution — kept consistent so both agree on who counts as a platform admin.
        public bool IsPlatformAdmin()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("platform_admin")?.Value == "true";
        }

        public TenantId GetCurrentTenantId()
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value;

            if (!Guid.TryParse(tenantIdClaim, out var tenantId) || tenantId == Guid.Empty)
                throw new UnauthorizedException("Tenant context is missing or invalid.");

            return new TenantId(tenantId);
        }
    }
}