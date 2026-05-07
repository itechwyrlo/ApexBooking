using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.SharedKernel.ValueObject
{
    public class ValueObjectTenantIdentifier
    {
        [NotMapped]
       public record TenantId(Guid Value);
    }
}