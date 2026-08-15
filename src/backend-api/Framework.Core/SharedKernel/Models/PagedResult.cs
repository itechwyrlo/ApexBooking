namespace ApexBooking.SharedKernel.Models
{
    public record PagedResult<T>(List<T> Data, int Total);
}