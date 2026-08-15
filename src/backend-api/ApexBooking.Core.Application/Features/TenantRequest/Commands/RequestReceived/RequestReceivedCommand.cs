using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.TenantRequest.Commands.RequestReceived
{
    public record RequestReceivedCommand(
    string businessName,
    BusinessType businessType,
    string slug,
    string ownerFirstName,
    string onwerLastName,
    string ownerEmail,
    SubscriptionPlanType requestedPlan) : ICommand;
}