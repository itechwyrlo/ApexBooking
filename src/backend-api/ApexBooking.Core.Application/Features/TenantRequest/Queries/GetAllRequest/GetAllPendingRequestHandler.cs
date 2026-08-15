using ApexBooking.Core.Application.Dtos.Request;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;
using AutoMapper;

namespace ApexBooking.Core.Application.Features.TenantRequest.Queries.GetAllRequest
{
    public class GetAllPendingRequestHandler
        : IQueryHandler<GetAllPendingRequestQuery, QueryResult<PendingTenantRequestSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllPendingRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<QueryResult<PendingTenantRequestSummary>> Handle(
            GetAllPendingRequestQuery query,
            CancellationToken cancellationToken)
        {
            var pagedResult = await _unitOfWork.TenantRegistrationRequestRepository.GetPageAsync(
                query.param,
                predicate: x => x.Status == TenantRequestStatus.pending,
                includes: null);

            var mappedItems = _mapper.Map<IEnumerable<PendingTenantRequestSummary>>(pagedResult.data);

            return new QueryResult<PendingTenantRequestSummary>(mappedItems, pagedResult.total);
        }
    }
}