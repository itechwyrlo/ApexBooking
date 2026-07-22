namespace ApexBooking.Core.Domain.Services.Cookie
{
    public interface ICookieService
    {
        string GetRefreshTokenFromCookie();
        void SetRefreshTokenCookie(string refreshToken);
        void DeleteRefreshTokenCookie();
    }
}