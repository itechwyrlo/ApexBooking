using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Repositories;

public class CustomerRepository(ApexBookingDbContext context)
    : GenericRepository<Customer>(context), ICustomerRepository
{
    private ApexBookingDbContext ApexBookingDbContext => Context as ApexBookingDbContext
        ?? throw new InvalidOperationException("The repository context is not an ApexBookingDbContext.");

    public async Task<IEnumerable<Customer>> GetByBusinessIdAsync(TenantId tenantId)
    {
        return await context.Customers
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Contact.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Customer>> SearchByNameOrPhoneAsync(TenantId tenantId, string term)
    {
        var normalized = (term ?? string.Empty).Trim();

        return await context.Customers
            .Where(c => c.TenantId == tenantId &&
                        (c.Contact.Name.Contains(normalized) ||
                         (c.Contact.PhoneNumber != null && c.Contact.PhoneNumber.Contains(normalized)) ||
                         (c.Contact.Email != null && c.Contact.Email.Contains(normalized))))
            .OrderBy(c => c.Contact.Name)
            .ToListAsync();
    }

    public async Task<Customer?> FindByEmailOrPhoneAsync(TenantId tenantId, string? email, string? phone)
    {
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        if (normalizedEmail is null && normalizedPhone is null)
            return null;

        return await context.Customers
            .FirstOrDefaultAsync(c => c.TenantId == tenantId &&
                ((normalizedEmail != null && c.Contact.Email == normalizedEmail) ||
                 (normalizedPhone != null && c.Contact.PhoneNumber == normalizedPhone)));
    }
}
