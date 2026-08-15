using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Authentication.ResetPassword
{
    /// <summary>
    /// Consumes the token from the emailed setup/reset link. The link carries the user id because
    /// an ASP.NET Identity reset token is user-scoped and cannot be resolved to a user on its own.
    /// </summary>
    public record ResetPasswordCommand(
        Guid UserId,
        string Token,
        string NewPassword,
        string ConfirmPassword) : ICommand<ResetPasswordResult>;

    /// <summary>
    /// Everything the SPA needs to drop the user straight onto their dashboard: a live session
    /// plus where to send them. The refresh token is never included here — like login, it goes
    /// straight into the HttpOnly cookie and never into the response body.
    /// </summary>
    public sealed record ResetPasswordResult(
        string AccessToken,
        string RedirectUrl,
        string Email,
        string FullName);
}
