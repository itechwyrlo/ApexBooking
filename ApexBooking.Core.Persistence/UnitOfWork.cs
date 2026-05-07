using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.Core.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ApexBooking.Core.Persistence
{
    public class UnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        private readonly ApexBookingDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        // Lazy holders for our repositories
        private readonly Lazy<ITenantRepository> _tenantRepository;
        private readonly Lazy<IUserRepository> _userRepository;
        private readonly Lazy<ISuperAdminRepository> _superAdminRepository;
        private readonly Lazy<IServiceRepository> _serviceRepository;
        private readonly Lazy<IResourceRepository> _resourceRepository;
        private readonly Lazy<IBookingRepository> _bookingRepository;

        public IResourceRepository ResourceRepository => _resourceRepository.Value;
        public IBookingRepository BookingRepository => _bookingRepository.Value;

        public ITenantRepository TenantRepository => _tenantRepository.Value;
        public IUserRepository UserRepository => _userRepository.Value;
        public ISuperAdminRepository SuperAdminRepository => _superAdminRepository.Value;
        public IServiceRepository ServiceRepository => _serviceRepository.Value;
        
        // Identity managers
        public UserManager<User> UserManager { get; }
        public RoleManager<IdentityRole<Guid>> RoleManager { get; }

        public UnitOfWork(ApexBookingDbContext context, IServiceProvider serviceProvider, UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            RoleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));

            _tenantRepository = new Lazy<ITenantRepository>(() => new TenantRepository(_context));
            _userRepository = new Lazy<IUserRepository>(() => new UserRepository(_context, UserManager));
            _superAdminRepository = new Lazy<ISuperAdminRepository>(() => new SuperAdminRepository(_context));
            _serviceRepository = new Lazy<IServiceRepository>(() => new ServiceRepository(_context));
            _resourceRepository = new Lazy<IResourceRepository>(() => new ResourceRepository(_context));
            _bookingRepository = new Lazy<IBookingRepository>(() => new BookingRepository(_context));
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<int> CompleteAsync(CancellationToken cancellationToken)
        {
            return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public ValueTask DisposeAsync()
        {
            return _context.DisposeAsync();
        }
    }
}