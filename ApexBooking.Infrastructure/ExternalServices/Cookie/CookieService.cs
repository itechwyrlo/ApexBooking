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

        public string GetRefreshTokenFromCookie()
        {
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];
            return token ?? string.Empty;
        }

        public void SetRefreshTokenCookie(string refreshToken)
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Append("refreshToken", refreshToken, BuildCookieOptions(expire: false));
        }

        public void DeleteRefreshTokenCookie()
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Append("refreshToken", "", BuildCookieOptions(expire: true));
        }

        private CookieOptions BuildCookieOptions(bool expire)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = _securityOptions.RequireHttps,
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
