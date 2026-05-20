using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Settings.Queries.GetTenantSettings
{
    internal sealed class GetTenantSettingsQueryHandler
        : IQueryHandler<GetTenantSettingsQuery, TenantSettingsDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetTenantSettingsQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<TenantSettingsDto> Handle(GetTenantSettingsQuery query, CancellationToken ct)
        {
            var tenantId = _contextService.GetCurrentTenantId();

            var tenant = await _unitOfWork.TenantRepository
                .GetAsync(
                    predicate: t => t.TenantId == tenantId,
                    includes: t => t.TenantSettings);

            if (tenant is null)
                throw new NotFoundException("Tenant settings not found.");

            return tenant.TenantSettings.ToSettingsDto();
        }
    }
}