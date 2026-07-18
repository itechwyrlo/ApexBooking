using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.SharedKernel.Services
{
    public interface ITenantEntity
    {
        TenantId TenantId { get; }
    }
}