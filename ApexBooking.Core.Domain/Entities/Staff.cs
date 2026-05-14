using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities
{
    public class Staff : ITenantEntity, IAggregateRoot
    {
        public StaffId StaffId { get; protected set; } = default!;
        public TenantId TenantId { get; private set; } = default!;
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email {get; private set;}
        public string ContactNumber {get; private set;}
        public string Description { get; private set; }
        public int Capacity { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        private readonly List<StaffAvailabilitySchedule> _availabilitySchedules = new();
        public IReadOnlyCollection<StaffAvailabilitySchedule> AvailabilitySchedules => _availabilitySchedules.AsReadOnly();

        private readonly List<StaffAvailabilityException> _availabilityExceptions = new();
        public IReadOnlyCollection<StaffAvailabilityException> AvailabilityExceptions => _availabilityExceptions.AsReadOnly();

        protected Staff() { }
        private Staff(TenantId tenantId, string firstname, string lastname, string email, string contactNumber, string description, int capacity)
        {
            StaffId = new StaffId(Guid.NewGuid());
            TenantId = tenantId;
            FirstName = firstname;
            LastName = lastname;
            Email = email;
            ContactNumber = contactNumber;
            Description = description;
            Capacity = capacity;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }


        public static Staff Create(TenantId tenantId, string firstname, string lastname, string email, string contactNumber, int capacity, string? description = null)
        {
            if (tenantId is null)
                throw new BusinessRuleBrokenException("Tenant is required.");

            if (string.IsNullOrWhiteSpace(lastname))
                throw new BusinessRuleBrokenException("Staff name is required.");

            if (capacity < 1)
                throw new BusinessRuleBrokenException("Capacity must be at least 1.");

            return new Staff(tenantId, firstname, lastname, email, contactNumber, description, capacity);
        }

        public void Update(string email, string contactNumber, string? description, int capacity)
        {
            if (capacity < 1)
                throw new BusinessRuleBrokenException("Capacity must be at least 1.");
            Email = email;
            ContactNumber = contactNumber;
            Description = description;
            Capacity = capacity;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive)
                throw new BusinessRuleBrokenException("Staff is already inactive.");

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive)
                throw new BusinessRuleBrokenException("Staff is already active.");

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetWeeklySchedule(IEnumerable<StaffAvailabilitySchedule> schedules)
        {
            var list = schedules.ToList();

            if (list.Count == 0)
                throw new BusinessRuleBrokenException("At least one schedule entry is required.");

            var duplicateDays = list.GroupBy(s => s.DayOfWeek).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateDays.Count != 0)
                throw new BusinessRuleBrokenException($"Duplicate day entries found: {string.Join(", ", duplicateDays)}.");

            foreach (var schedule in list.Where(s => s.IsAvailable))
            {
                if (schedule.StartTime >= schedule.EndTime)
                    throw new BusinessRuleBrokenException($"Start time must be before end time on {schedule.DayOfWeek}.");
            }

            _availabilitySchedules.Clear();
            _availabilitySchedules.AddRange(list);
            UpdatedAt = DateTime.UtcNow;
        }

        public StaffAvailabilitySchedule? GetScheduleForDay(DayOfWeek dayOfWeek)
       => _availabilitySchedules.FirstOrDefault(s => s.DayOfWeek == dayOfWeek);


        public void AddException(DateOnly exceptionDate, ExceptionType exceptionType, TimeOnly? startTime, TimeOnly? endTime, string? note)
        {
            if (exceptionDate <= DateOnly.FromDateTime(DateTime.UtcNow))
                throw new BusinessRuleBrokenException("Exception date must be a future date.");

            bool duplicate = _availabilityExceptions.Any(e => e.ExceptionDate == exceptionDate && e.ExceptionType == exceptionType);
            if (duplicate)
                throw new BusinessRuleBrokenException($"An exception of type '{exceptionType}' already exists for {exceptionDate}.");

            if (exceptionType != ExceptionType.UnavailableAllDay)
            {
                if (startTime is null || endTime is null)
                    throw new BusinessRuleBrokenException("Start time and end time are required for this exception type.");

                if (startTime >= endTime)
                    throw new BusinessRuleBrokenException("Start time must be before end time.");
            }

            var exception = StaffAvailabilityException.Create(StaffId, TenantId, exceptionDate, exceptionType, startTime, endTime, note);
            _availabilityExceptions.Add(exception);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveException(StaffAvailabilityExceptionId exceptionId)
        {
            var exception = _availabilityExceptions.FirstOrDefault(e => e.StaffAvailabilityExceptionId == exceptionId);
            if (exception is null)
                throw new BusinessRuleBrokenException("Exception not found.");

            _availabilityExceptions.Remove(exception);
            UpdatedAt = DateTime.UtcNow;
        }


    }
}