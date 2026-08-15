namespace ApexBooking.Core.Domain.Services.Cookie
{
    public interface ICookieService
    {
        // isPlatformAdmin selects which of the two independent cookies to operate on
        // ("refreshToken" for tenant sessions, "superadminRefreshToken" for platform-admin
        // sessions) — kept separate so a login of one kind, in one browser tab, can never
        // overwrite the other kind's session in a different tab.
        string GetRefreshTokenFromCookie(bool isPlatformAdmin);
        void SetRefreshTokenCookie(string refreshToken, bool isPlatformAdmin);
        void DeleteRefreshTokenCookie(bool isPlatformAdmin);
    }
}