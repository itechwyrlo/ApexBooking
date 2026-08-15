using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ScheduleBooking
{
    public class ScheduleBookingHandler : ICommandHandler<ScheduleBookingCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;
        private readonly IUserContextService _userContext;

        public ScheduleBookingHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity, IUserContextService userContext)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
            _userContext = userContext;
        }

        public async Task<Guid> Handle(ScheduleBookingCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to create appointment. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Branches, t => t.Members, t => t.Services, t => t.Bookings]);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to create appointment. Isolated tenant context could not be verified.");

            // A plain Staff member may only create walk-in appointments for their own assigned
            // branch — Owner/Admin manage the whole tenant and may pick any of its branches.
            var currentUserId = _userContext.GetCurrentUserId();
            var callerMember = tenant.Members.FirstOrDefault(m => m.UserId == currentUserId && m.IsActive);
            if (callerMember is { Role: SystemRole.Staff } && callerMember.BranchId.Value != command.BranchId)
                throw new BusinessRuleBrokenException("You can only create walk-in appointments for your own assigned branch.");

            // Customer Profile Matching: reuse an existing customer (matched by email/phone within
            // this tenant) or create a new one, same allocation strategy as the public booking wizard.
            var customerName = $"{command.CustomerFirstName} {command.CustomerLastName}".Trim();
            var customer = await _unitOfWork.CustomerRepository.FindByEmailOrPhoneAsync(
                tenant.TenantId, command.CustomerEmail, command.CustomerPhone);

            if (customer is null)
            {
                customer = Customer.Create(tenant.TenantId, new CustomerContact(customerName, command.CustomerEmail, command.CustomerPhone));
                _unitOfWork.CustomerRepository.Add(customer);
            }

            var booking = tenant.ScheduleBooking(
                branchId: new BranchId(command.BranchId),
                customerId: customer.CustomerId,
                staffId: new TenantMemberId(command.StaffId),
                serviceId: new ServiceId(command.ServiceId),
                date: command.ScheduledDate,
                startTime: command.ScheduledStartTime,
                customerNotes: command.CustomerNotes,
                admitImmediately: command.AdmitImmediately
            );

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return booking.BookingId.Value;
        }
    }
}
