using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.SharedKernel.Models
{
    public record SortParam
{
    public bool? SortOrderDescending { get; set; }
    
    public required string OrderProperty { get; set; }
}
}