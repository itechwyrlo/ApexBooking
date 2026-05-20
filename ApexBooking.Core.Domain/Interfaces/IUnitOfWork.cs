using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using Microsoft.AspNetCore.Identity;

namespace ApexBooking.Core.Domain.Interfaces;

public interface IUnitOfWork
{
    ITenantRepository TenantRepository { get; }
    IUserRepository UserRepository { get; }
    ISuperAdminRepository SuperAdminRepository { get; }
    IServiceRepository ServiceRepository { get; }
    IStaffRepository StaffRepository { get; }
    IBookingRepository BookingRepository { get; }
    IPaymentTransactionRepository PaymentTransactionRepository { get; }
    IPlatformPaymentGatewayRepository PlatformPaymentGatewayRepository { get; }
    IGuestCancellationTokenRepository GuestCancellationTokenRepository { get; }
    ITenantRequestRepository TenantRequestRepository { get; }
    INotificationRepository NotificationRepository { get; }
    Task<int> CompleteAsync();
    Task<int> CompleteAsync(CancellationToken cancellationToken);
}