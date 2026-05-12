using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Slug,
    string BusinessName,
    string OwnerFullName,
    string OwnerEmail,
    string OwnerPhone,
    string AdminPassword
) : ICommand<BaseResponse<OrganizationSummaryDto>>;
