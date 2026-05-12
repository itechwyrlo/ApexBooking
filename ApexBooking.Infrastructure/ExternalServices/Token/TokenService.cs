using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApexBooking.Infrastructure.ExternalServices.Token;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _opts;
    private readonly IMemoryCache _cache;

    public JwtTokenService(IOptions<JwtOptions> opts, IMemoryCache cache)
    {
        _opts = opts.Value;
        _cache = cache;
    }

    public string GenerateAccessToken(User user, string role, string tenantSlug)
    {
        var creds = BuildSigningCredentials();
        var jti = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("tenant_id", user.TenantId.Value.ToString()),
            new("role", role),
            new("email", user.Email!),
            new("email_verified", user.EmailConfirmed.ToString().ToLower(), ClaimValueTypes.Boolean),
            new(JwtRegisteredClaimNames.Jti, jti)
        };

        return WriteToken(claims);
    }

    public string GenerateAccessToken(SuperAdmin superAdmin)
    {
        var jti = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new("sub", superAdmin.SuperAdminId.Value.ToString()),
            new("role", "superadmin"),
            new("email", superAdmin.Email),
            new(JwtRegisteredClaimNames.Jti, jti)
        };

        return WriteToken(claims);
    }

    public string GenerateRefreshTokenRaw()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLower();
    }

    public bool IsJtiBlacklisted(string jti)
    {
        return _cache.TryGetValue($"jti:{jti}", out _);
    }

    public Task BlacklistJtiAsync(string jti, CancellationToken ct)
    {
        var expiry = TimeSpan.FromMinutes(_opts.AccessTokenExpiryMinutes + 1);
        _cache.Set($"jti:{jti}", "1", expiry);
        return Task.CompletedTask;
    }

    private SigningCredentials BuildSigningCredentials()
    {
        var rsa = RSA.Create();
        if (!string.IsNullOrEmpty(_opts.PrivateKeyPem))
            rsa.ImportFromPem(_opts.PrivateKeyPem);
        return new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
    }

    private string WriteToken(List<Claim> claims)
    {
        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_opts.AccessTokenExpiryMinutes),
            signingCredentials: BuildSigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
