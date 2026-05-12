namespace ApexBooking.WebApi.Dtos;

public record CreateTenantUserRequestDto(
    string FullName,
    string Email,
    string Role
);

public record AssignExistingUserRequestDto(
    string Email,
    string Role
);
