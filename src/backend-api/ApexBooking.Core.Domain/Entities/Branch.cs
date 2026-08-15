using System;
using System.Collections.Generic;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities
{
    // Mutators are internal so all writes route through the Tenant aggregate root — same posture as Service/TenantMember.
    public sealed class Branch : ITenantEntity
    {
        public BranchId BranchId { get; private set; } = default!;
        public TenantId TenantId { get; private set; } = default!;
        public string BranchName { get; private set; } = string.Empty;
        public Address Address { get; private set; } = default!;
        public string TimeZoneId { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private readonly List<DayScheduleEntry> _operatingHours = [];
        public IReadOnlyCollection<DayScheduleEntry> OperatingHours => _operatingHours.AsReadOnly();

        private Branch() { }

        internal static Branch Create(TenantId tenantId, string branchName, string timeZoneId,
                                      Address? address, DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(branchName))
                throw new BusinessRuleBrokenException("Branch name is required.");
            if (string.IsNullOrWhiteSpace(timeZoneId) || !Services.BranchTimeZoneConverter.IsValidTimeZoneId(timeZoneId))
                throw new BusinessRuleBrokenException("A valid time zone is required for each physical location.");

            var branch = new Branch
            {
                BranchId = new BranchId(Guid.NewGuid()),
                TenantId = tenantId,
                BranchName = branchName.Trim(),
                TimeZoneId = timeZoneId,
                Address = address ?? Address.Empty,
                IsActive = true,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };

            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
                branch._operatingHours.Add(DayScheduleEntry.OffFor(day));

            return branch;
        }

        internal void UpdateProfile(string branchName, string timeZoneId, Address address)
        {
            if (string.IsNullOrWhiteSpace(branchName))
                throw new BusinessRuleBrokenException("Branch name is required.");
            if (string.IsNullOrWhiteSpace(timeZoneId) || !Services.BranchTimeZoneConverter.IsValidTimeZoneId(timeZoneId))
                throw new BusinessRuleBrokenException("A valid time zone is required for each physical location.");

            BranchName = branchName.Trim();
            TimeZoneId = timeZoneId;
            Address = address ?? throw new ArgumentNullException(nameof(address));
            UpdatedAt = DateTime.UtcNow;
        }

        internal void SetOperatingHours(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isOff)
        {
            var entry = new DayScheduleEntry(dayOfWeek, startTime, endTime, isOff); // self-validating
            _operatingHours.RemoveAll(h => h.DayOfWeek == dayOfWeek);
            _operatingHours.Add(entry);
            UpdatedAt = DateTime.UtcNow;
        }

        internal void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
