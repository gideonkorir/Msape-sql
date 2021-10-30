using Automatonymous;
using Divergic.Logging.Xunit;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.TestFramework.Logging;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Msape.BookKeeping.Components.Tests
{
    public abstract class StateMachineTest<TStateMachine, TInstance> : IAsyncLifetime
        where TStateMachine : MassTransitStateMachine<TInstance>
        where TInstance: class, SagaStateMachineInstance
    {
        protected readonly InMemoryTestHarness _testHarness;
        protected readonly IServiceProvider _provider;
        protected readonly IStateMachineSagaTestHarness<TInstance, TStateMachine> _sagaHarness;

        protected virtual TimeSpan TestTimeout => TimeSpan.FromSeconds(15);

        protected StateMachineTest(ITestOutputHelper testOutputHelper)
        {
            var services = new ServiceCollection();
            services
                .AddSingleton<ILoggerFactory>(provider => LogFactory.Create(testOutputHelper))
                .AddLogging()
                .AddMassTransitInMemoryTestHarness(cfg =>
                {
                    cfg.AddSagaStateMachine<TStateMachine, TInstance>()
                        .InMemoryRepository();

                    cfg.AddPublishMessageScheduler();

                    cfg.AddSagaStateMachineTestHarness<TStateMachine, TInstance>();

                    ConfigureMassTransit(cfg);
                });
            ConfigureServices(services);
            _provider = services.BuildServiceProvider(true);
            _testHarness = _provider.GetRequiredService<InMemoryTestHarness>();
            _testHarness.TestTimeout = TestTimeout;
            _testHarness.OnConfigureInMemoryReceiveEndpoint += (config) =>
            {
                config.StateMachineSaga<TInstance>(_provider);
            };
            _sagaHarness = _provider.GetRequiredService<IStateMachineSagaTestHarness<TInstance, TStateMachine>>();
        }

        public async Task<ISendEndpoint> GetSagaEndpoint()
            => await _testHarness.GetSendEndpoint(new Uri("queue:PostTransaction")); //not sure why

        public async Task SendToSaga<T>(T message)
        {
            var endpoint = await GetSagaEndpoint();
            await endpoint.Send(message);
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
        }

        protected virtual void ConfigureMassTransit(IServiceCollectionBusConfigurator configurator)
        {
        }

        public virtual async Task InitializeAsync()
        {
            await _testHarness.Start();
        }

        public virtual async Task DisposeAsync()
        {
            await _testHarness.Stop();
        }
    }
}
