using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.TenantRequest.Commands.RequestReceived
{
    internal sealed class RequestReceivedHandler : ICommandHandler<RequestReceivedCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        public RequestReceivedHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(RequestReceivedCommand command, CancellationToken cancellationToken)
        {
            var newRequest = TenantRegistrationRequest.Create(
                command.businessName,
                command.businessType,
                command.slug,
                command.ownerFirstName,
                command.onwerLastName,
                command.ownerEmail,
                command.requestedPlan);

            _unitOfWork.TenantRegistrationRequestRepository.Add(newRequest);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}