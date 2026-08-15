namespace ApexBooking.Core.Application.Dtos.Response
{
    public record TenantBookingCountsDto(int Pending, int CheckedIn, int Completed, int Missed);
}
