using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Paymongo; // Namespace matching your injection layer
using ApexBooking.Core.Domain.Services.Ticketing;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.InitiateBooking
{
    public class InitiateBookingHandler : ICommandHandler<InitiateBookingCommand, BookingInitiationResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPayMongoService _payMongoService;
        private readonly ITicketTokenService _ticketTokenService;
        private readonly IQrCodeGenerator _qrCodeGenerator;
        private readonly ITenantEntity _tenantEntity;

        public InitiateBookingHandler(
            IUnitOfWork unitOfWork,
            IPayMongoService payMongoService,
            ITicketTokenService ticketTokenService,
            IQrCodeGenerator qrCodeGenerator,
            ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _payMongoService = payMongoService;
            _ticketTokenService = ticketTokenService;
            _qrCodeGenerator = qrCodeGenerator;
            _tenantEntity = tenantEntity;
        }

        public async Task<BookingInitiationResult> Handle(InitiateBookingCommand command, CancellationToken cancellationToken)
        {
            // 1. Single Database Trip: Load the active tenant along with branch/staff/service/booking
            // context AND payment policy/credentials in one round-trip.
            // NOTE: Tenant does not implement ITenantEntity, so the global EF Core query filter does
            // NOT cover this lookup — scope explicitly via the ambient ITenantEntity (resolved by
            // TenantMiddleware from the JWT tenant_id claim, or by slug path segment for anonymous traffic).
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Booking transaction failed. No tenant context could be resolved for this request.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.PaymentPolicy!, t => t.PaymentCredential!, t => t.BookingPolicy!,
                           t => t.Branches, t => t.Members, t => t.Services, t => t.Bookings]);

            if (tenant == null || tenant.PaymentPolicy == null)
                throw new BusinessRuleBrokenException("Booking transaction failed. Business workspace details could not be resolved.");

            // 2. Resolve Step 1's branch selection
            var branch = tenant.Branches.FirstOrDefault(b => b.BranchId.Value == command.BranchId && b.IsActive)
                ?? throw new BusinessRuleBrokenException("The selected branch is unavailable.");

            // 3. Hydrate structural staff and service details from the aggregate tracking boundary
            var staff = tenant.Members.FirstOrDefault(m => m.TenantMemberId.Value == command.StaffId);
            var service = tenant.Services.FirstOrDefault(s => s.ServiceId.Value == command.ServiceId);

            if (staff == null || !staff.IsActive || service == null || !service.IsActive)
                throw new BusinessRuleBrokenException("The selected staff member or service catalog configuration is currently unavailable.");

            // 🌟 INVARIANT GUARD: the selected staff must actually be deployed to the chosen branch
            if (staff.BranchId != branch.BranchId)
                throw new BusinessRuleBrokenException("The selected staff member is not deployed to the chosen branch.");

            // 🌟 INVARIANT GUARD: independently re-verify the minimum-advance-booking lead time
            // server-side — don't just trust that the slots-listing endpoint's filtering was honored
            // by the caller. This service's own override wins over the tenant-wide BookingPolicy default.
            int minAdvanceBookingHours = service.MinAdvanceBookingHoursOverride
                ?? tenant.BookingPolicy?.MinAdvanceBookingHours
                ?? 0;

            var nowBranchLocal = BranchTimeZoneConverter.ToBranchLocalTime(DateTime.UtcNow, branch.TimeZoneId);
            var earliestBookableMoment = nowBranchLocal.AddHours(minAdvanceBookingHours);
            var requestedMoment = command.ScheduledDate.ToDateTime(command.ScheduledStartTime);

            if (requestedMoment < earliestBookableMoment)
                throw new BusinessRuleBrokenException($"This service requires at least {minAdvanceBookingHours} hour(s) of advance notice.");

            // 4. Customer Profile Matching / Allocation Strategy — reuse an existing customer
            // (matched by email/phone within this tenant) or create a new one from the wizard's
            // contact fields, instead of discarding them behind a throwaway random id.
            var customerName = $"{command.CustomerFirstName} {command.CustomerLastName}".Trim();
            var customer = await _unitOfWork.CustomerRepository.FindByEmailOrPhoneAsync(
                tenant.TenantId, command.CustomerEmail, command.CustomerPhone);

            if (customer is null)
            {
                customer = Customer.Create(tenant.TenantId, new CustomerContact(customerName, command.CustomerEmail, command.CustomerPhone));
                _unitOfWork.CustomerRepository.Add(customer);
            }

            var allocatedCustomerId = customer.CustomerId;

            // 5. Calculate Financial Metrics based on the Refactored PaymentPolicy
            bool requiresUpfrontPayment = tenant.PaymentPolicy.RequirementType != PaymentRequirementType.None;
            decimal amountToCharge = 0m;

            if (requiresUpfrontPayment)
            {
                if (tenant.PaymentPolicy.RequirementType == PaymentRequirementType.FullPaymentRequired)
                {
                    amountToCharge = service.Price;
                }
                else if (tenant.PaymentPolicy.RequirementType == PaymentRequirementType.DepositRequired)
                {
                    amountToCharge = tenant.PaymentPolicy.DepositType == DepositType.Percentage
                        ? (service.Price * (tenant.PaymentPolicy.DepositValue / 100m))
                        : tenant.PaymentPolicy.DepositValue;
                }
            }
            else
            {
                // No upfront payment required — the full service price is still owed, just
                // collected in person at the visit. Surfaced back to the customer via
                // BookingInitiationResult.AmountToPay so the success screen can tell them
                // how much to bring.
                amountToCharge = service.Price;
            }

            // 🌟 INVARIANT VERIFICATION GUARD: If payment is required, defensively enforce that this tenant
            // has successfully saved operational, enabled credentials to block runtime connection crashes
            if (requiresUpfrontPayment && amountToCharge > 0)
            {
                if (tenant.PaymentCredential == null || !tenant.PaymentCredential.IsEnabled)
                    throw new BusinessRuleBrokenException("Online payments are currently unavailable for this business workspace. Please select 'Pay at Counter' or contact support.");
            }

            // 6. Critical Invariant Fix: route through the aggregate root rather than mutating the
            // read-only Bookings collection by cast — this honors field-backed collections and
            // tracks domain event lifecycles safely.
            var booking = tenant.PlaceCustomerBooking(
                branchId: branch.BranchId,
                customerId: allocatedCustomerId,
                staffId: staff.TenantMemberId,
                serviceId: service.ServiceId,
                date: command.ScheduledDate,
                startTime: command.ScheduledStartTime,
                customerNotes: command.CustomerNotes,
                requiresUpfrontPayment: requiresUpfrontPayment,
                amountDue: amountToCharge);

            // 7. Execute PayMongo Server-to-Server Link Generation if upfront payment is required
            string? payMongoQrUrl = null;
            string? payMongoCheckoutUrl = null;

            if (requiresUpfrontPayment && amountToCharge > 0)
            {
                var paymentSourcePayload = await _payMongoService.CreatePaymentSourceAsync(
                    tenantSecretKey: tenant.PaymentCredential!.SecretKey,
                    bookingId: booking.BookingId.Value,
                    branchId: branch.BranchId.Value,
                    amountPhp: amountToCharge,
                    description: $"{service.Name} at {branch.BranchName} with {staff.FirstName}",
                    cancellationToken: cancellationToken
                );

                payMongoQrUrl = paymentSourcePayload.QrCodeUrl;
                payMongoCheckoutUrl = paymentSourcePayload.CheckoutUrl;
            }

            // 8. Track modifications and commit atomically down to the data access layer
            _unitOfWork.TenantRepository.Update(tenant);

            // 🌟 TRANSACTION COMMIT LINE:
            // If payment wasn't required, the BookingScheduledDomainEvent raised inside the factory
            // is intercepted and executed completely during this Save method automatically!
            await _unitOfWork.CompleteAsync(cancellationToken);

            // 🌟 Issue the signed QR boarding-pass token so the wizard's success screen can render it on-the-spot
            var ticketToken = _ticketTokenService.Issue(new TicketPayload(booking.BookingId, tenant.TenantId, branch.BranchId));
            var ticketQrCodeDataUri = $"data:image/png;base64,{Convert.ToBase64String(_qrCodeGenerator.GeneratePng(ticketToken))}";

            return new BookingInitiationResult(
                BookingId: booking.BookingId.Value,
                BookingReference: booking.BookingReference,
                RequiresPayment: requiresUpfrontPayment,
                AmountToPay: amountToCharge,
                PayMongoQrCodeUrl: payMongoQrUrl,
                PayMongoCheckoutUrl: payMongoCheckoutUrl,
                TicketToken: ticketToken,
                TicketQrCodeDataUri: ticketQrCodeDataUri
            );
        }
    }
}
