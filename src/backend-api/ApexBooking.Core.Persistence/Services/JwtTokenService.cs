using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Services.Auth;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Persistence.CustomClaimTypes;
using ApexBooking.Core.Persistence.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Services
{
    public class JwtTokenService : ITokenService
    {
        private readonly JwtSettings _options;

        public JwtTokenService(IOptions<JwtSettings> options)
        {
            _options = options.Value;
        }

        public string GenerateAccessToken(TokenDescriptor descriptor)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(_options.PrivateKeyPem);

            // A fresh RSA instance is created (and disposed via `using`) on every call, but
            // Microsoft.IdentityModel.Tokens caches signature providers by key material by
            // default (CacheSignatureProviders = true) — a later call can be handed back a
            // cached provider that still points at this call's already-disposed RSA object,
            // surfacing as "ObjectDisposedException: RSABCrypt". Disable caching for this
            // short-lived key so every call gets its own provider.
            var signingKey = new RsaSecurityKey(rsa)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            };

            var signingCredentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.RsaSha256);

            var claims = BuildClaims(descriptor);

            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiryMinutes),
                signingCredentials: signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public TokenPrincipal? ValidateExpiredAccessToken(string accessToken)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(_options.PublicKeyPem);

            // Same disposed-key caching hazard as GenerateAccessToken above — disable provider
            // caching for this short-lived key.
            var validationKey = new RsaSecurityKey(rsa)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            };

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = validationKey,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero
            };

            var handler = new JwtSecurityTokenHandler();

            try
            {
                var principal = handler.ValidateToken(accessToken, validationParameters, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwt)
                    return null;

                if (!jwt.Header.Alg.Equals(SecurityAlgorithms.RsaSha256, StringComparison.Ordinal))
                    return null;

                return MapPrincipal(principal);
            }
            catch
            {
                return null;
            }
        }

        // --- Streamlined Claims Mapping Adapters ---
        private static IReadOnlyCollection<Claim> BuildClaims(TokenDescriptor descriptor)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, descriptor.UserId.ToString()),
                new(ClaimTypes.NameIdentifier, descriptor.UserId.ToString()),
                new(ClaimTypes.Email, descriptor.Email),
                new(ClaimTypes.Name, descriptor.FullName),
                new(JwtClaimTypes.PlatformAdmin, descriptor.IsPlatformAdmin.ToString().ToLowerInvariant())
            };

            // Safely unwrap the value object to string
            if (descriptor.TenantId is not null)
            {
                claims.Add(new Claim(
                    JwtClaimTypes.TenantId,
                    descriptor.TenantId.Value.ToString())); // Extracts raw Guid out of TenantId value object wrapper
            }

            // Write the exact SystemRole Enum string representation to standard ASP.NET Role claims matrix
            if (descriptor.Role is not null)
            {
                claims.Add(new Claim(JwtClaimTypes.TenantRole, descriptor.Role.Value.ToString()));
                claims.Add(new Claim(ClaimTypes.Role, descriptor.Role.Value.ToString())); // Added for out-of-the-box [Authorize(Roles="Owner")] support
            }

            if (descriptor.Slug is not null)
            {
                claims.Add(new Claim(JwtClaimTypes.TenantSlug, descriptor.Slug));
            }

            if (descriptor.TenantMemberId is not null)
            {
                claims.Add(new Claim(
                    JwtClaimTypes.TenantMemberId,
                    descriptor.TenantMemberId.Value.ToString())); // Extracts raw Guid out of TenantMemberId value object wrapper
            }

            return claims;
        }

        private static TokenPrincipal MapPrincipal(ClaimsPrincipal principal)
        {
            var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? throw new InvalidOperationException("User ID claim is missing."));

            var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            var fullName = principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
            
            bool.TryParse(principal.FindFirst(JwtClaimTypes.PlatformAdmin)?.Value, out var isPlatformAdmin);

            // Reconstruct the strongly-typed TenantId wrapper object from the claim string
            TenantId? tenantId = null;
            var tenantIdClaim = principal.FindFirst(JwtClaimTypes.TenantId)?.Value;
            if (!string.IsNullOrWhiteSpace(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantGuid))
            {
                tenantId = new TenantId(tenantGuid);
            }

            // Parse string claim back to domain SystemRole Enum natively
            SystemRole? tenantRole = null;
            var tenantRoleClaim = principal.FindFirst(JwtClaimTypes.TenantRole)?.Value 
                ?? principal.FindFirst(ClaimTypes.Role)?.Value; // Fallback check
                
            if (!string.IsNullOrWhiteSpace(tenantRoleClaim) && Enum.TryParse<SystemRole>(tenantRoleClaim, true, out var roleEnum))
            {
                tenantRole = roleEnum;
            }

            var slug = principal.FindFirst(JwtClaimTypes.TenantSlug)?.Value;

            TenantMemberId? tenantMemberId = null;
            var tenantMemberIdClaim = principal.FindFirst(JwtClaimTypes.TenantMemberId)?.Value;
            if (!string.IsNullOrWhiteSpace(tenantMemberIdClaim) && Guid.TryParse(tenantMemberIdClaim, out var memberGuid))
            {
                tenantMemberId = new TenantMemberId(memberGuid);
            }

            return new TokenPrincipal(
                userId,
                email,
                fullName,
                isPlatformAdmin,
                tenantId,
                tenantRole,
                slug,
                tenantMemberId);
        }
    }
}
