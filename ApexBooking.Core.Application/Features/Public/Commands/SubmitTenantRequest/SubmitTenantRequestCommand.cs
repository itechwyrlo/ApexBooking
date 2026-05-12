using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Public.Commands.SubmitTenantRequest;

public sealed record SubmitTenantRequestCommand(
    string BusinessName,
    string OwnerFullName,
    string OwnerEmail,
    string OwnerPhone,
    string Plan,
    string? Message
) : ICommand<BaseResponse<Guid>>;
