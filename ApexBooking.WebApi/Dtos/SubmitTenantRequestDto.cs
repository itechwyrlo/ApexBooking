namespace ApexBooking.WebApi.Dtos;

public sealed record SubmitTenantRequestDto(
    string BusinessName,
    string OwnerFullName,
    string OwnerEmail,
    string OwnerPhone,
    string Plan,
    string? Message
);
