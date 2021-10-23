using MassTransit;
using MassTransit.Definition;
using GreenPipes;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting.Saga
{
    public class PostTransactionSagaDefinition : SagaDefinition<PostTransactionSaga>
    {
        public PostTransactionSagaDefinition()
        {
            EndpointName = "transaction-posting";
        }
        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<PostTransactionSaga> sagaConfigurator)
        {
            endpointConfigurator.UseServiceBusMessageScheduler();

            endpointConfigurator.UseMessageRetry(retry =>
            {
                retry.Interval(3, TimeSpan.FromSeconds(5));
            });

            endpointConfigurator.UseInMemoryOutbox();
        }
    }
}
