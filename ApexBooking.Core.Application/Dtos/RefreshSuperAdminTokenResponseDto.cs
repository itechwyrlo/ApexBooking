namespace ApexBooking.Core.Application.Dtos;

public sealed record RefreshSuperAdminTokenResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public Guid UserId { get; init; }
}
