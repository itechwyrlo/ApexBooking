using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Public.Commands.SubmitTenantRequest;

public sealed record SubmitTenantRequestCommand(
    string BusinessName,
    string OwnerFullName,
    string OwnerEmail,
    string OwnerPhone,
    string Plan,
    string? Message
) : ICommand<Guid>;
