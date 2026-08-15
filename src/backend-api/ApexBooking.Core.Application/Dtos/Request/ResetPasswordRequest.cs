namespace ApexBooking.Core.Application.Dtos.Request
{
    public sealed record ResetPasswordRequestDto(
        Guid UserId,
        string Token,
        string NewPassword,
        string ConfirmPassword);
}


