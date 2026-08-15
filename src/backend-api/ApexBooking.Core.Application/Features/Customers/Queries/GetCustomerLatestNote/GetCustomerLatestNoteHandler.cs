using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Customers.Queries.GetCustomerLatestNote
{
    public class GetCustomerLatestNoteHandler : IQueryHandler<GetCustomerLatestNoteQuery, CustomerLatestNoteDto?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetCustomerLatestNoteHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<CustomerLatestNoteDto?> Handle(GetCustomerLatestNoteQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load client notes. No authenticated tenant context was found.");

            var row = await _unitOfWork.TenantRepository.GetLatestStaffNoteAsync(
                tenantId,
                new CustomerId(query.CustomerId),
                cancellationToken);

            return row is null ? null : new CustomerLatestNoteDto(row.Notes, row.NotedOn);
        }
    }
}
