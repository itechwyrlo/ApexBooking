namespace ApexBooking.WebApi.Dtos;

public record CreateOrganizationRequestDto(
    string Slug,
    string BusinessName,
    string OwnerFullName,
    string OwnerEmail,
    string OwnerPhone,
    string AdminPassword
);
