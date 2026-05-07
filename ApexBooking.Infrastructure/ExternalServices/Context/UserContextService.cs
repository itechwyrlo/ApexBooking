using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
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
                throw new UnauthorizedAccessException("User is not authenticated.");

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
        public string GetCurrentJti()
        {
            // JwtRegisteredClaimNames.Jti usually maps to "jti"
            var jti = _httpContextAccessor.HttpContext?.User
                .FindFirstValue("jti");

            if (string.IsNullOrEmpty(jti))
                throw new UnauthorizedAccessException("Token identifier (JTI) is missing.");

            return jti;
        }

        public TenantId GetCurrentTenantId()
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value;

            if (!Guid.TryParse(tenantIdClaim, out var tenantId) || tenantId == Guid.Empty)
                throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

            return new TenantId(tenantId);
        }
    }
}