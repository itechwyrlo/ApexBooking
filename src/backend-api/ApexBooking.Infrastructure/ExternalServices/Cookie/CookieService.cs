using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ApexBooking.Infrastructure.ExternalServices.Cookie
{
    public class CookieService : ICookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SecurityOptions _securityOptions;

        public CookieService(IHttpContextAccessor httpContextAccessor, IOptions<SecurityOptions> securityOptions)
        {
            _httpContextAccessor = httpContextAccessor;
            _securityOptions = securityOptions.Value;
        }

        private static string CookieName(bool isPlatformAdmin) => isPlatformAdmin ? "superadminRefreshToken" : "refreshToken";

        public string GetRefreshTokenFromCookie(bool isPlatformAdmin)
        {
            var token = _httpContextAccessor.HttpContext?.Request.Cookies[CookieName(isPlatformAdmin)];
            return token ?? string.Empty;
        }

        public void SetRefreshTokenCookie(string refreshToken, bool isPlatformAdmin)
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(CookieName(isPlatformAdmin), refreshToken, BuildCookieOptions(expire: false));
        }

        public void DeleteRefreshTokenCookie(bool isPlatformAdmin)
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(CookieName(isPlatformAdmin), "", BuildCookieOptions(expire: true));
        }

        private CookieOptions BuildCookieOptions(bool expire)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = _securityOptions.SecureCookies,
                SameSite = SameSiteMode.Strict,
                Expires = expire
                    ? DateTime.UtcNow.AddDays(-1)
                    : DateTime.UtcNow.AddDays(7)
            };

            if (!string.IsNullOrWhiteSpace(_securityOptions.CookieDomain))
                options.Domain = _securityOptions.CookieDomain;

            return options;
        }
    }
}
