using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.WebApi.Dtos
{
    public class CreateResourceRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public int ResourceType { get; set; }
        public int Capacity { get; set; }
        public string? Description { get; set; }
    }
}