namespace ApexBooking.WebApi.Dtos;

public record AcceptInvitationRequestDto(
    string Token,
    string NewPassword,
    string ConfirmPassword
);
