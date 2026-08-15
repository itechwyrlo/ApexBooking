using ApexBooking.Core.Application.Dependency;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Persistence.Dependencies;
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
    public void AddPersistenceServices_registers_the_unit_of_work()
    {
        // Post-commit domain-event dispatch lives in UnitOfWork.CompleteAsync (ADR-062), not an interceptor.
        var services = new ServiceCollection();
        var config = Config(new()
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=Test;Trusted_Connection=True;"
        });

        services.AddPersistenceServices(config);

        Assert.Contains(services, d => d.ServiceType == typeof(IUnitOfWork));
    }
}
