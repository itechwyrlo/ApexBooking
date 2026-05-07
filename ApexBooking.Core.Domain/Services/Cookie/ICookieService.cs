using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Domain.Services.Cookie
{
    public interface ICookieService
    {
        string GetRefreshTokenFromCookie();
        void SetRefreshTokenCookie(string refreshToken);
        void DeleteRefreshTokenCookie();
    }
}