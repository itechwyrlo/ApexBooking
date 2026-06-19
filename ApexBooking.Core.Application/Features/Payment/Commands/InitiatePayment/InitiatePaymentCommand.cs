using System;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Payment.Commands.InitiatePayment
{
    public sealed record InitiatePaymentCommand(Guid BookingId) : ICommand<InitiatePaymentDto>;
}