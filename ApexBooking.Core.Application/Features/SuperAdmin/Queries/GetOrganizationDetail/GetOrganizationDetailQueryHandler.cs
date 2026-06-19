using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetOrganizationDetail;

internal sealed class GetOrganizationDetailQueryHandler
    : IQueryHandler<GetOrganizationDetailQuery, OrganizationDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOrganizationDetailQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrganizationDetailDto> Handle(
        GetOrganizationDetailQuery query,
        CancellationToken ct)
    {
        var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);
        if (tenant is null)
            throw new NotFoundException("Organization not found.");

        var users = await _unitOfWork.UserRepository.GetAllByTenantAsync(tenant.TenantId);
        var bookings = await _unitOfWork.BookingRepository.GetAllAsync(t => t.TenantId == tenant.TenantId);
        var services = await _unitOfWork.ServiceRepository.GetAllAsync(t => t.TenantId == tenant.TenantId);

        var staffCount = users.Count(u => u.Role == UserRole.Staff);
        var clientCount = users.Count(u => u.Role == UserRole.Customer);

        var userDtos = users.Select(u => u.ToTenantUserDto()).ToList();

        return tenant.ToOrganizationDetailDto(
            bookingCount: bookings.Count(),
            serviceCount: services.Count(),
            staffCount: staffCount,
            clientCount: clientCount,
            users: userDtos);
    }
}
