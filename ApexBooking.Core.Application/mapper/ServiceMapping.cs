using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.Services.Mappings;

public static class ServiceMappings
{
    public static ServiceDto ToServiceDto(this Service service) => new()
    {
        Id = service.ServiceId.Value,
        Name = service.Name,
        Description = service.Description,
        DurationMinutes = service.DurationMinutes,
        BufferBeforeMinutes = service.BufferBeforeMinutes,
        BufferAfterMinutes = service.BufferAfterMinutes,
        Price = service.Price,
        CurrencyCode = service.CurrencyCode,
        MinAdvanceBookingHours = service.MinAdvanceBookingHours,
        MaxAdvanceBookingDays = service.MaxAdvanceBookingDays,
        IsActive = service.IsActive,
        ResourceIds = service.ServiceResources
            .Select(sr => sr.ResourceId.Value)
            .ToList(),
        CreatedAt = service.CreatedAt,
        UpdatedAt = service.UpdatedAt
    };

    public static PublicServiceDto ToPublicServiceDto(this Service service) => new(
        service.ServiceId.Value,
        service.Name,
        service.Description,
        service.DurationMinutes,
        service.Price,
        service.CurrencyCode
    );

    
}