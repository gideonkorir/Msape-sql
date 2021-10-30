using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.TestFramework.Logging;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Msape.BookKeeping.Data.EF;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Msape.BookKeeping.Components.Tests
{
    public abstract class ConsumerTest<T> : IAsyncLifetime where T : class, IConsumer
    {
        protected readonly InMemoryTestHarness _testHarness;
        protected readonly IServiceProvider _provider;
        protected readonly IConsumerTestHarness<T> _consumerHarness;
        protected readonly IBusRegistrationContext _busRegistrationContext;

        protected virtual TimeSpan TestTimeout => TimeSpan.FromSeconds(15);

        protected ConsumerTest()
        {
            var services = new ServiceCollection();
            services
                //.AddSingleton<ILoggerFactory>(provider => new TestOutputLoggerFactory(true))
                .AddMassTransitInMemoryTestHarness(cfg =>
                {
                    cfg.AddConsumer<T>();

                    cfg.AddPublishMessageScheduler();

                    cfg.AddConsumerTestHarness<T>();

                    ConfigureMassTransit(cfg);
                });
            ConfigureServices(services);
            _provider = services.BuildServiceProvider(true);
            _testHarness = _provider.GetRequiredService<InMemoryTestHarness>();
            _busRegistrationContext = _provider.GetRequiredService<IBusRegistrationContext>();
            _testHarness.TestTimeout = TestTimeout;
            _testHarness.OnConfigureInMemoryReceiveEndpoint += (config) =>
            {
                config.ConfigureConsumer<T>(_busRegistrationContext);
            };
            _consumerHarness = _provider.GetRequiredService<IConsumerTestHarness<T>>();
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<BookKeepingContext>(opts =>
            {
                opts.UseInMemoryDatabase(GetType().Name, config =>
                {
                });
            });
        }

        protected  virtual void ConfigureMassTransit(IServiceCollectionBusConfigurator configurator)
        {
        }

        protected async Task WithContext(Func<BookKeepingContext, Task> func)
        {
            using var scope = _provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<BookKeepingContext>();
            await func(service).ConfigureAwait(false);
        }

        public virtual async Task InitializeAsync()
        {
            await SeedContext();
            await _testHarness.Start();
        }

        private async Task SeedContext()
        {
            using var scope = _provider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<BookKeepingContext>();
            await SeedContext(context).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        protected virtual Task SeedContext(BookKeepingContext context)
            => Task.CompletedTask;

        public virtual async Task DisposeAsync()
        {
            await _testHarness.Stop();
        }
    }
}
