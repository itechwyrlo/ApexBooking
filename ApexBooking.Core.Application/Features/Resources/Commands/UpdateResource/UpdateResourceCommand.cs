using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Resources.Commands.UpdateResource
{
    public sealed record UpdateResourceCommand(
        Guid ResourceId,
        string Name,
        string? Description,
        int Capacity
    ) : ICommand<BaseResponse<ResourceId>>;
}