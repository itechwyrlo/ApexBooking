using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.ApproveTenantRequest;

public sealed record ApproveTenantRequestCommand(
    Guid RequestId,
    string Slug,
    int TrialDays = 14
) : ICommand;
