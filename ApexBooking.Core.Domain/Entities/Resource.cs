// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using ApexBooking.Core.Domain.Enums;
// using ApexBooking.Core.Domain.ValueObjects;
// using ApexBooking.SharedKernel.Exceptions;
// using ApexBooking.SharedKernel.Models;
// using ApexBooking.SharedKernel.Services;
// using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

// namespace ApexBooking.Core.Domain.Entities;

// public class Resource : IAggregateRoot, ITenantEntity
// {
//     public StaffId StaffId { get; protected set; } = default!;
//     public TenantId TenantId { get; private set; } = default!;
//     public string Name { get; private set; } = string.Empty;
//     public string? Description { get; private set; }
//     public ResourceType ResourceType { get; private set; }
//     public int Capacity { get; private set; }
//     public bool IsActive { get; private set; }
//     public DateTime CreatedAt { get; private set; }
//     public DateTime UpdatedAt { get; private set; }

//     // Children: availability schedules — one per day of week (max 7)
//     private readonly List<StaffAvailabilitySchedule> _availabilitySchedules = new();
//     public IReadOnlyCollection<StaffAvailabilitySchedule> AvailabilitySchedules => _availabilitySchedules.AsReadOnly();

//     // Children: date-specific exceptions
//     private readonly List<StaffAvailabilityException> _availabilityExceptions = new();
//     public IReadOnlyCollection<StaffAvailabilityException> AvailabilityExceptions => _availabilityExceptions.AsReadOnly();

//     protected Resource() { }

//     private Resource(TenantId tenantId, string name, string? description, ResourceType resourceType, int capacity)
//     {
//         StaffId = new StaffId(Guid.NewGuid());
//         TenantId = tenantId;
//         Name = name;
//         Description = description;
//         ResourceType = resourceType;
//         Capacity = capacity;
//         IsActive = true;
//         CreatedAt = DateTime.UtcNow;
//         UpdatedAt = DateTime.UtcNow;
//     }

//     public static Resource Create(TenantId tenantId, string name, ResourceType resourceType, int capacity, string? description = null)
//     {
//         if (tenantId is null)
//             throw new BusinessRuleBrokenException("Tenant is required.");

//         if (string.IsNullOrWhiteSpace(name))
//             throw new BusinessRuleBrokenException("Resource name is required.");

//         if (capacity < 1)
//             throw new BusinessRuleBrokenException("Capacity must be at least 1.");

//         return new Resource(tenantId, name, description, resourceType, capacity);
//     }

//     public void Update(string name, string? description, int capacity)
//     {
//         if (string.IsNullOrWhiteSpace(name))
//             throw new BusinessRuleBrokenException("Resource name is required.");

//         if (capacity < 1)
//             throw new BusinessRuleBrokenException("Capacity must be at least 1.");

//         Name = name;
//         Description = description;
//         Capacity = capacity;
//         UpdatedAt = DateTime.UtcNow;
//     }

//     public void Deactivate()
//     {
//         if (!IsActive)
//             throw new BusinessRuleBrokenException("Resource is already inactive.");

//         IsActive = false;
//         UpdatedAt = DateTime.UtcNow;
//     }

//     public void Activate()
//     {
//         if (IsActive)
//             throw new BusinessRuleBrokenException("Resource is already active.");

//         IsActive = true;
//         UpdatedAt = DateTime.UtcNow;
//     }

//     // ── Availability Schedule Management ────────────────────────────────────

//     /// <summary>
//     /// Replaces the full weekly availability schedule.
//     /// TR-7.4: this is a full replacement — existing schedules are cleared and
//     /// replaced in a single operation.
//     /// </summary>
//     public void SetWeeklySchedule(IEnumerable<StaffAvailabilitySchedule> schedules)
//     {
//         var list = schedules.ToList();

//         if (list.Count == 0)
//             throw new BusinessRuleBrokenException("At least one schedule entry is required.");

//         var duplicateDays = list.GroupBy(s => s.DayOfWeek).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
//         if (duplicateDays.Count != 0)
//             throw new BusinessRuleBrokenException($"Duplicate day entries found: {string.Join(", ", duplicateDays)}.");

//         foreach (var schedule in list.Where(s => s.IsAvailable))
//         {
//             if (schedule.StartTime >= schedule.EndTime)
//                 throw new BusinessRuleBrokenException($"Start time must be before end time on {schedule.DayOfWeek}.");
//         }

//         _availabilitySchedules.Clear();
//         _availabilitySchedules.AddRange(list);
//         UpdatedAt = DateTime.UtcNow;
//     }

//     public StaffAvailabilitySchedule? GetScheduleForDay(DayOfWeek dayOfWeek)
//         => _availabilitySchedules.FirstOrDefault(s => s.DayOfWeek == dayOfWeek);

//     // ── Availability Exception Management ────────────────────────────────────

//     /// <summary>
//     /// Adds a date-specific exception that overrides the base schedule.
//     /// TR-7.5: exception_date must be a future date. Duplicate date + type raises 409.
//     /// </summary>
//     public void AddException(DateOnly exceptionDate, ExceptionType exceptionType, TimeOnly? startTime, TimeOnly? endTime, string? note)
//     {
//         if (exceptionDate <= DateOnly.FromDateTime(DateTime.UtcNow))
//             throw new BusinessRuleBrokenException("Exception date must be a future date.");

//         bool duplicate = _availabilityExceptions.Any(e => e.ExceptionDate == exceptionDate && e.ExceptionType == exceptionType);
//         if (duplicate)
//             throw new BusinessRuleBrokenException($"An exception of type '{exceptionType}' already exists for {exceptionDate}.");

//         if (exceptionType != ExceptionType.UnavailableAllDay)
//         {
//             if (startTime is null || endTime is null)
//                 throw new BusinessRuleBrokenException("Start time and end time are required for this exception type.");

//             if (startTime >= endTime)
//                 throw new BusinessRuleBrokenException("Start time must be before end time.");
//         }

//         var exception = StaffAvailabilityException.Create(StaffId, TenantId, exceptionDate, exceptionType, startTime, endTime, note);
//         _availabilityExceptions.Add(exception);
//         UpdatedAt = DateTime.UtcNow;
//     }

//     public void RemoveException(StaffAvailabilityExceptionId exceptionId)
//     {
//         var exception = _availabilityExceptions.FirstOrDefault(e => e.StaffAvailabilityExceptionId == exceptionId);
//         if (exception is null)
//             throw new BusinessRuleBrokenException("Exception not found.");

//         _availabilityExceptions.Remove(exception);
//         UpdatedAt = DateTime.UtcNow;
//     }
// }
