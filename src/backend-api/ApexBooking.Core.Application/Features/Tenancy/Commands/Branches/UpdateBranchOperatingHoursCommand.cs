using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos.Request;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.Branches
{
    public record UpdateBranchOperatingHoursCommand(
        Guid BranchId,
        IReadOnlyCollection<OperatingHoursUpdateItem> OperatingHours
    ) : ICommand;
}
