using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.SharedKernel.Models
{
    public record PagedResult<T>(List<T> Data, int Total);
}