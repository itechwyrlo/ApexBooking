using System;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.Branches
{
    public record AddBranchCommand(
        string BranchName,
        string TimeZoneId,
        string Street,
        string? Barangay,
        string City,
        string Province,
        string ZipCode
    ) : ICommand<Guid>;
}
