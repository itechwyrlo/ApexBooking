using System;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Platform.Commands.RetryOutboxMessage
{
    public record RetryOutboxMessageCommand(Guid Id) : ICommand;
}
