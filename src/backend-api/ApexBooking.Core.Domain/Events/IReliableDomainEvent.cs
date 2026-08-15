using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Domain.Events;

/// <summary>
/// Marks a domain event whose handler(s) make an external call (email, SMS, push) and therefore
/// need at-least-once delivery guarantees. Events implementing this are extracted into an
/// <see cref="Entities.OutboxMessage"/> row in the same transaction as the business write
/// (see UnitOfWork.CompleteAsync) instead of being dispatched synchronously in-request.
///
/// Events that do NOT implement this (e.g. TenantRequestApproveDomainEvent, which drives
/// ProvisionTenantOnRequestApprovedHandler's business logic, not an external call) keep going
/// through the existing synchronous post-commit dispatch, unchanged.
/// </summary>
public interface IReliableDomainEvent : IDomainEvent
{
}
