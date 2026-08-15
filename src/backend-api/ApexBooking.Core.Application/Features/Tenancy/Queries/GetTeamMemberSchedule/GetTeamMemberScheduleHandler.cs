using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetTeamMemberSchedule
{
    public class GetTeamMemberScheduleHandler : IQueryHandler<GetTeamMemberScheduleQuery, IReadOnlyCollection<DayScheduleSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetTeamMemberScheduleHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<DayScheduleSummary>> Handle(GetTeamMemberScheduleQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Workspace scope could not be verified.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members);

            if (tenant is null)
                throw new BusinessRuleBrokenException("Workspace scope could not be verified.");

            var member = tenant.Members.FirstOrDefault(m => m.TenantMemberId.Value == query.TenantMemberId)
                ?? throw new BusinessRuleBrokenException("Target team member record not found within this business workspace.");

            return member.WeeklySchedule
                .OrderBy(s => (int)s.DayOfWeek)
                .Select(s => new DayScheduleSummary(s.DayOfWeek, s.StartTime, s.EndTime, s.IsOff))
                .ToList();
        }
    }
}
