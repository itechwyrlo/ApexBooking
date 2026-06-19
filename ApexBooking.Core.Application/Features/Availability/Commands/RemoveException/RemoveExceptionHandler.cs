using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Availability.Commands.RemoveException
{
    internal sealed class RemoveExceptionHandler : ICommandHandler<RemoveExceptionCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public RemoveExceptionHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task Handle(RemoveExceptionCommand command, CancellationToken ct)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var staffId = new StaffId(command.StaffId);

            var staff = await _unitOfWork.StaffRepository
                .FindByIdWithAvailabilityAsync(staffId, ct)
                .ConfigureAwait(false);

            if (staff is null || staff.TenantId != tenantId)
                throw new NotFoundException("Staff not found.");

            var exceptionId = new StaffAvailabilityExceptionId(command.ExceptionId);
            staff.RemoveException(exceptionId);

            _unitOfWork.StaffRepository.Update(staff);
            await _unitOfWork.CompleteAsync(ct);
        }
    }
}