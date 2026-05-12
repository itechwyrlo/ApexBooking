using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.ApproveTenantRequest;

public sealed record ApproveTenantRequestCommand(
    Guid RequestId,
    string Slug,
    int TrialDays = 14
) : ICommand<BaseResponse<bool>>;
