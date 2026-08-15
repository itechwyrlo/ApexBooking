using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Domain.Events;

// Customer Management domain events (02-architecture-blueprint.md §7).

public record CustomerCreatedDomainEvent(CustomerId CustomerId) : IDomainEvent;

public record CustomerUpdatedDomainEvent(CustomerId CustomerId) : IDomainEvent;
