using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Availability.Commands.AddException
{
    internal sealed class AddExceptionHandler : ICommandHandler<AddExceptionCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public AddExceptionHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task Handle(AddExceptionCommand command, CancellationToken ct)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var staffId = new StaffId(command.ResourceId);

            var staff = await _unitOfWork.StaffRepository
                .FindByIdWithAvailabilityAsync(staffId, ct)
                .ConfigureAwait(false);

            if (staff is null || staff.TenantId != tenantId)
                throw new NotFoundException("Staff not found.");

            staff.AddException(
                command.ExceptionDate,
                command.ExceptionType,
                command.StartTime,
                command.EndTime,
                command.Note
            );

            _unitOfWork.StaffRepository.Update(staff);
            await _unitOfWork.CompleteAsync(ct);
        }
    }
}