using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Dtos.Request
{
    public record RejectTenantRequest(string Reason);
}