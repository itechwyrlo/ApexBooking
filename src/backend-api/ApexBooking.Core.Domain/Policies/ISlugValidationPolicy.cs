namespace ApexBooking.Core.Domain.Policies
{
    public interface ISlugValidationPolicy
    {
        bool IsValid(string slug);
    }
}
