using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Domain.Entities
{
    public class TenantRegistrationRequest : IAggregateRoot, IHasDomainEvents
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public TenantRegistrationRequestId TenantRegistrationRequestId { get; protected set; } = default!;

        // --- Organization Info ---
        public string BusinessName { get; private set; } = string.Empty;
        public BusinessType BusinessType { get; private set; }
        public string RequestedSlug { get; private set; } = string.Empty;
        public SubscriptionPlanType RequestedPlan { get; private set; }

        // --- Requester / Owner Info (Best Practice Updates) ---
        public string OwnerFirstName { get; private set; } = string.Empty;
        public string OwnerLastName { get; private set; } = string.Empty;
        public string OwnerEmail { get; private set; } = string.Empty;

        // --- Audit & Governance ---
        public TenantRequestStatus Status { get; private set; }
        public DateTime RequestedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; private set; }
        public Guid? ReviewedByUserId { get; private set; } //Platform admin userId
        public string? RejectionReason { get; private set; }

        public IReadOnlyCollection<IDomainEvent> DomainEvents
            => _domainEvents.AsReadOnly();

        public void ClearDomainEvents() => _domainEvents.Clear();

        public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

        private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        private TenantRegistrationRequest() { }

        // Updated Constructor accepting Owner Identity fields
        public TenantRegistrationRequest(
            string businessName,
            BusinessType businessType,
            string slug,
            string ownerFirstName,
            string ownerLastName,
            string ownerEmail,
            SubscriptionPlanType requestedPlan)
        {
            if (string.IsNullOrWhiteSpace(businessName)) throw new ArgumentException("Business name is required.");
            if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug is required.");
            if (string.IsNullOrWhiteSpace(ownerFirstName)) throw new ArgumentException("Owner first name is required.");
            if (string.IsNullOrWhiteSpace(ownerLastName)) throw new ArgumentException("Owner last name is required.");
            if (string.IsNullOrWhiteSpace(ownerEmail)) throw new ArgumentException("Owner email is required.");

            TenantRegistrationRequestId = new TenantRegistrationRequestId(Guid.NewGuid());
            BusinessName = businessName;
            BusinessType = businessType;
            RequestedSlug = slug.ToLower().Trim();
            RequestedPlan = requestedPlan;

            // Assigning personal identity details
            OwnerFirstName = ownerFirstName.Trim();
            OwnerLastName = ownerLastName.Trim();
            OwnerEmail = ownerEmail.ToLower().Trim();

            Status = TenantRequestStatus.pending;

            AddDomainEvent(new TenantRequestReceivedDomainEvent(
                TenantRegistrationRequestId,
                BusinessName,
                RequestedPlan,
                OwnerFirstName,
                OwnerLastName,
                OwnerEmail,
                RequestedAt));
        }

        public static TenantRegistrationRequest Create(
        string businessName,
        BusinessType businessType,
        string slug,
        string ownerFirstName,
        string ownerLastName,
        string ownerEmail,
        SubscriptionPlanType requestedPlan)
        {
            return new TenantRegistrationRequest(
                businessName,
                businessType,
                slug,
                ownerFirstName,
                ownerLastName,
                ownerEmail,
                requestedPlan);


        }

        public void Approve(Guid adminUserId)
        {
            if (Status != TenantRequestStatus.pending)
                throw new InvalidOperationException("Only pending requests can be approved.");

            var utcNow = DateTime.UtcNow;

            Status = TenantRequestStatus.approved;
            ReviewedAt = utcNow;
            ReviewedByUserId = adminUserId;

            AddDomainEvent(new TenantRequestApproveDomainEvent(
                TenantRegistrationRequestId,
                BusinessName,
                BusinessType,
                RequestedSlug,
                RequestedPlan,
                OwnerFirstName,
                OwnerLastName,
                OwnerEmail,
                adminUserId,
                utcNow));
        }

        public void Reject(Guid adminUserId, string reason)
        {
            if (Status != TenantRequestStatus.pending)
                throw new InvalidOperationException("Only pending requests can be rejected.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A rejection reason must be provided.");

            var utcNow = DateTime.UtcNow;

            Status = TenantRequestStatus.rejected;
            RejectionReason = reason.Trim();
            ReviewedAt = utcNow;
            ReviewedByUserId = adminUserId;

            AddDomainEvent(new TenantRequestRejectedDomainEvent(
                TenantRegistrationRequestId,
                BusinessName,
                OwnerFirstName,
                OwnerLastName,
                OwnerEmail,
                RejectionReason,
                adminUserId,
                utcNow));
        }
    }
}
