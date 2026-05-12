using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.RejectTenantRequest;

public sealed record RejectTenantRequestCommand(
    Guid RequestId,
    string Reason
) : ICommand<BaseResponse<bool>>;
