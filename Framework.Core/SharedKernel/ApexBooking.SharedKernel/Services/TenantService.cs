using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.SharedKernel.Services
{
    public class TenantService : ITenantService
    {
        public TenantId? TenantId { get; set; }
    }
}