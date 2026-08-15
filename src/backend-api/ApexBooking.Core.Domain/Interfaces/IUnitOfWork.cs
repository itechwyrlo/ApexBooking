using ApexBooking.Core.Domain.Repositories;

namespace ApexBooking.Core.Domain.Interfaces;

public interface IUnitOfWork
{
    ITenantRepository TenantRepository { get; }
    ITenantRegistrationRequestRepository TenantRegistrationRequestRepository {get;}
    ICustomerRepository CustomerRepository { get; }
    INotificationRepository NotificationRepository { get; }
    IFcmTokenRepository FcmTokenRepository { get; }
    Task<int> CompleteAsync();
    Task<int> CompleteAsync(CancellationToken cancellationToken);
}