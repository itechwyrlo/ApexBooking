using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.TimeOffs.Queries.GetTimeOffRequests
{
    public class GetTimeOffRequestsHandler : IQueryHandler<GetTimeOffRequestsQuery, QueryResult<TimeOffRequestSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;

        public GetTimeOffRequestsHandler(IUnitOfWork unitOfWork, IUserContextService userContext, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
        }

        public async Task<QueryResult<TimeOffRequestSummary>> Handle(GetTimeOffRequestsQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load time-off requests. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to load time-off requests. Isolated tenant context could not be verified.");

            var currentUserId = _userContext.GetCurrentUserId();
            var currentMember = tenant.Members.FirstOrDefault(m => m.UserId == currentUserId)
                ?? throw new BusinessRuleBrokenException("Your account is not linked to an active staff account.");

            // Only Owner sees the whole team's requests; every other role — Admin, Staff, and any
            // future role added without an explicit widen — is scoped to their own requests only.
            var isManagement = currentMember.Role == SystemRole.Owner;

            var allRequests = tenant.Members
                .Where(m => isManagement || m.TenantMemberId == currentMember.TenantMemberId)
                .SelectMany(m => m.TimeOffRequests.Select(r => new TimeOffRequestSummary(
                    r.Id.Value,
                    m.TenantMemberId.Value,
                    $"{m.FirstName} {m.LastName}".Trim(),
                    m.PhotoUrl,
                    r.Type,
                    r.StartDate,
                    r.EndDate,
                    r.StartTime,
                    r.EndTime,
                    r.Status,
                    r.Reason,
                    r.RequestedAt,
                    r.DecidedAt)))
                .Where(r => query.Status == null || r.Status == query.Status)
                .OrderByDescending(r => r.RequestedAt)
                .ToList();

            var page = allRequests
                .Skip((query.Param.PageNumber - 1) * query.Param.PageSize)
                .Take(query.Param.PageSize)
                .ToList();

            return new QueryResult<TimeOffRequestSummary>(page, allRequests.Count);
        }
    }
}
