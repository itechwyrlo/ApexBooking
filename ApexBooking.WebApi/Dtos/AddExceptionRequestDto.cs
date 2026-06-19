using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.WebApi.Dtos
{
    public class AddExceptionRequestDto
    {
        public DateOnly ExceptionDate { get; set; }
        public string ExceptionType { get; set; } = string.Empty;
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? Note { get; set; }
    }
}