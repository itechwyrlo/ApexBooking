using System.ComponentModel.DataAnnotations.Schema;

namespace ApexBooking.SharedKernel.ValueObject
{
    public class ValueObjectTenantIdentifier
    {
        [NotMapped]
       public record TenantId(Guid Value);
    }
}