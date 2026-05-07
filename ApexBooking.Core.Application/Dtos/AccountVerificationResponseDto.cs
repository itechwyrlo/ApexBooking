using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record AccountVerificationResponseDto
    {
        public string Url { get; init; }
        public string? TenantSlug { get; init; }

    }
}