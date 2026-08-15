namespace ApexBooking.Core.Application.Dtos.Response
{
    // Tells the caller, up front, whether removing this team member will hard-delete or
    // soft-delete (deactivate) them — lets the UI show the right confirmation message before
    // the user commits, rather than surprising them after the fact.
    public record TeamMemberRemovalImpact(bool HasHistoricalRecords);
}
