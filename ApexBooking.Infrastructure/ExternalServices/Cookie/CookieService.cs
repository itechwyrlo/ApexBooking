using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Services.Cookie;
using Microsoft.AspNetCore.Http;

namespace ApexBooking.Infrastructure.ExternalServices.Cookie
{
    public class CookieService : ICookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetRefreshTokenFromCookie()
        {
            // Access the Request object to read incoming cookies
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];

            return token ?? string.Empty;
        }

        public void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            _httpContextAccessor.HttpContext?.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }

        public void DeleteRefreshTokenCookie()
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                // Setting the expiration to the past tells the browser to delete it immediately
                Expires = DateTime.UtcNow.AddDays(-1)
            };

            _httpContextAccessor.HttpContext?.Response.Cookies.Append("refreshToken", "", cookieOptions);
        }
    }
}