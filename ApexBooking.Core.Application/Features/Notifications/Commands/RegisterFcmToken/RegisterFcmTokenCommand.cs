using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Notifications.Commands.RegisterFcmToken;

public sealed record RegisterFcmTokenCommand(string Token) : ICommand;
