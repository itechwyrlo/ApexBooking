using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetBookingPolicy
{
    public class GetBookingPolicyHandler : IQueryHandler<GetBookingPolicyQuery, BookingPolicyDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetBookingPolicyHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<BookingPolicyDto> Handle(GetBookingPolicyQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load scheduling rules. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.BookingPolicy!
            );

            if (tenant == null || tenant.BookingPolicy == null)
                throw new BusinessRuleBrokenException("Failed to load scheduling rules. Policy record not found.");

            var policy = tenant.BookingPolicy;

            return new BookingPolicyDto(
                policy.BookingConfirmationMode,
                policy.MinAdvanceBookingHours,
                policy.MaxAdvanceBookingDays,
                policy.CancellationCutoffHours,
                policy.LateCancellationPolicy,
                policy.NotifyBookingConfirmed,
                policy.NotifyBookingCancelled,
                policy.NotifyBookingReminder,
                policy.NotifyNewCustomer,
                policy.ReminderHoursBefore
            );
        }
    }
}
