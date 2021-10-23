using MassTransit.Testing;
using Moq;
using Msape.BookKeeping.Components.Consumers.Posting;
using Msape.BookKeeping.Components.Infra;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Msape.BookKeeping.Components.Tests.Posting
{
    public class PostTransactionToSourceConsumerTest
    {
        private readonly Mock<ICosmosAccount> _mock;
        public readonly InMemoryTestHarness _harness;

        public PostTransactionToSourceConsumerTest()
        {
            _mock = new Mock<ICosmosAccount>();
            _harness = new InMemoryTestHarness();
            //_harness.Consumer(() => new PostTransactionToSourceConsumer(_mock.Object));
        }

        public void ConfigureMock(Action<Mock<ICosmosAccount>> configure)
        {
            configure(_mock);
        }
    }
}
