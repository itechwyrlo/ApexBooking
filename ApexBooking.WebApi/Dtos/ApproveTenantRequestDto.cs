namespace ApexBooking.WebApi.Dtos;

public sealed record ApproveTenantRequestDto(string Slug, int TrialDays = 14);

public sealed record RejectTenantRequestDto(string Reason);
