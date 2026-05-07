using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Domain.Services.TokenService
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user, string role, string tenantSlug);
        string GenerateRefreshTokenRaw();
        string HashToken(string raw);
        bool IsJtiBlacklisted(string jti);
        Task BlacklistJtiAsync(string jti, CancellationToken ct = default);
    }
}