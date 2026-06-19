using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Notifications.Commands.UnregisterFcmToken;

public sealed record UnregisterFcmTokenCommand(string Token) : ICommand;
