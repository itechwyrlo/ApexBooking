using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Tenants.Commands.UpdateTenantProfile
{
    internal sealed class UpdateTenantProfileCommandHandler : ICommandHandler<UpdateTenantProfileCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTenantProfileCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateTenantProfileCommand command, CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.Slug == command.TenantSlug,
                t => t.TenantProfile);

            if (tenant is null)
                throw new NotFoundException("Tenant not found.");

            if (tenant.TenantProfile is null)
                throw new NotFoundException("Tenant profile not found.");

            TimeFormat? timeFormat = command.TimeFormat switch
            {
                "12h" => TimeFormat.TwelveHour,
                "24h" => TimeFormat.TwentyFourHour,
                _ => null
            };

            tenant.TenantProfile.UpdateProfile(
                logoUrl: command.LogoUrl,
                addressLine1: command.AddressLine1,
                addressLine2: command.AddressLine2,
                city: command.City,
                state: command.State,
                postalCode: command.PostalCode,
                countryCode: command.CountryCode,
                timezone: command.Timezone,
                currencyCode: command.CurrencyCode,
                websiteUrl: command.WebsiteUrl,
                contactEmail: command.ContactEmail,
                contactPhone: command.ContactPhone,
                dateFormat: command.DateFormat,
                timeFormat: timeFormat,
                languageCode: command.LanguageCode);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}