using System.Collections.Generic;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record ForgotPasswordResponseDto
    {
        public string Message { get; init; }
    
    }
}
