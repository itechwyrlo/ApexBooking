namespace ApexBooking.Core.Application.Dtos.Response
{
    public record TeamMemberSummary(
        Guid TenantMemberId,
        Guid? UserId,
        string Email,
        string FirstName,
        string LastName,
        string FullName,
        string ContactNumber,
        string? PhotoUrl,
        string Role,
        string? CustomJobTitle,
        string Status,
        DateTime CreatedAt
    )
    {
        public TeamMemberSummary() : this(default, default, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, default, string.Empty, default, string.Empty, default) { }
    }
}