using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.SharedKernel.Models
{
    public record QueryObjectParams : PageParam
    {
         public List<SortParam> SortingParams { get; init; } = [];
    }
}