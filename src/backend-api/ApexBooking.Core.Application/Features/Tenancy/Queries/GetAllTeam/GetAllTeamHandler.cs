using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using AutoMapper;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetAllTeam
{
    public class GetAllTeamHandler : IQueryHandler<GetAllTeamQuery, QueryResult<TeamMemberSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantEntity _tenantEntity;

        public GetAllTeamHandler(IUnitOfWork unitOfWork, IMapper mapper, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantEntity = tenantEntity;
        }
        public async Task<QueryResult<TeamMemberSummary>> Handle(GetAllTeamQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load team. No authenticated tenant context was found.");

            var currentTenant = await _unitOfWork.TenantRepository.GetPageAsync(
                query.param,
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members
            );

            if (currentTenant == null)
                throw new BusinessRuleBrokenException("Failed to load team. Isolated business workspace could not be verified.");

            var orderedMembers = currentTenant.data
                .SelectMany(t => t.Members)
                .OrderBy(m => m.CreatedAt)
                .ToList();

            var mappedItems = _mapper.Map<IEnumerable<TeamMemberSummary>>(orderedMembers);

            return new QueryResult<TeamMemberSummary>(mappedItems, orderedMembers.Count);
        }
    }
}