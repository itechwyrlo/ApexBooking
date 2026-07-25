using ApexBooking.Core.Application.Dependency;
using ApexBooking.Core.Persistence.Dependencies;
using ApexBooking.Core.Persistence.Interceptors;
using ApexBooking.SharedKernel.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApexBooking.Core.Domain.UnitTests.DomainEvents;

public class DomainEventRegistrationTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void AddApplicationServices_registers_the_domain_event_dispatcher()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices(Config(new()));

        Assert.Contains(services, d => d.ServiceType == typeof(IDomainEventDispatcher));
    }

    [Fact]
    public void AddPersistenceServices_registers_the_dispatch_interceptor()
    {
        var services = new ServiceCollection();
        var config = Config(new()
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=Test;Trusted_Connection=True;"
        });

        services.AddPersistenceServices(config);

        Assert.Contains(services, d => d.ServiceType == typeof(DispatchDomainEventsInterceptor));
    }
}
