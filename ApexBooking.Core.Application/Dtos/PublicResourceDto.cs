using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Dtos
{
    public record PublicResourceDto(
        Guid ResourceId,
        string Name,
        string? Description
    );
}