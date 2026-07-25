using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Common.DomainEvent
{
   public record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) 
    : INotification where TDomainEvent : IDomainEvent;
}