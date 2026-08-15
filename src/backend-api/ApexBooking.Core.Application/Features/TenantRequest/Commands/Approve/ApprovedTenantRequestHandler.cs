using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.TenantRequest.Commands.Approve
{
    internal sealed class ApprovedTenantRequestHandler : ICommandHandler<ApprovedTenantRequestCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;

        public ApprovedTenantRequestHandler(
            IUnitOfWork unitOfWork,
            IUserContextService userContext)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
        }

        public async Task Handle(ApprovedTenantRequestCommand command, CancellationToken cancellationToken)
        {
            var currentUserId = _userContext.GetCurrentUserId();

            var request = await _unitOfWork.TenantRegistrationRequestRepository
                .GetByIdAsync(new TenantRegistrationRequestId(command.RequestId));

            if (request is null)
                throw new NotFoundException("Tenant registration request was not found.");

            if (request.Status != TenantRequestStatus.pending)
                throw new BusinessRuleBrokenException("This request has already been processed.");

            request.Approve(currentUserId);

            _unitOfWork.TenantRegistrationRequestRepository.Update(request);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
