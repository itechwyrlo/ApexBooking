using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Slug,
    string BusinessName,
    string OwnerFullName,
    string OwnerEmail,
    string OwnerPhone,
    string AdminPassword
) : ICommand<OrganizationSummaryDto>;
