using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.RejectTenantRequest;

public sealed record RejectTenantRequestCommand(
    Guid RequestId,
    string Reason
) : ICommand;
