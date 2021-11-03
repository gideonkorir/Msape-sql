using Microsoft.Extensions.DependencyInjection;
using Msape.BookKeeping.Components.Consumers.Posting;
using Msape.BookKeeping.Components.Consumers.Posting.Saga;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Msape.BookKeeping.Components.Tests.Posting
{
    public abstract class PostingStateMachineTest : StateMachineTest<PostTransactionStateMachine, PostTransactionSaga>
    {
        protected PostingStateMachineTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddSingleton(provider => new PostTransactionStateMachineOptions()
            {
                AccountTypeSendEndpoint = (type) => _testHarness.InputQueueAddress,
                TtlProvider = (type) => -1
            });
        }
    }

    public class When_TransactionPosted_Is_Received : PostingStateMachineTest
    {
        public When_TransactionPosted_Is_Received(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Then_An_Instance_Is_Created()
        {
            var message = new TransactionPostedToSource()
            {
                PostingId = Guid.NewGuid(),
                TransactionId = 45,
                IsContra = false,
                Amount = new MoneyInfo() { Currency = 1, Value = 100_000 },
                SourceAccount = new InternalContracts.AccountId()
                {
                    Id = 5,
                    SubjectId = 5,
                    AccountNumber = "acc 0",
                    AccountType = Data.AccountType.CustomerAccount,
                    Name = "cust"
                },
                DestAccount = new InternalContracts.AccountId()
                {
                    Id = 6,
                    SubjectId = 6,
                    AccountType = Data.AccountType.CustomerAccount,
                    AccountNumber = "acc 1",
                    Name = "cust"
                },
                Timestamp = DateTime.UtcNow,
                TransactionType = Data.TransactionType.CustomerSendMoney,
                SourceBalanceAfter = new MoneyInfo() { Currency = 1, Value = 50_000 },
                Charges = null
            };
            await SendToSaga(message);
            Assert.True(await _sagaHarness.Sagas.Any());
            Assert.True(await _sagaHarness.Created.Any(c => c.CorrelationId == message.PostingId));
            Assert.True(await _sagaHarness.Consumed.Any());
            var saga = _sagaHarness.Sagas.Contains(message.PostingId);
            Assert.Equal(message.TransactionId, saga.TransactionId);
            Assert.Equal(message.Amount, saga.Amount);
            Assert.Equal(message.TransactionType, saga.TransactionType);
            Assert.True(SagaInstanceHelper.HasSameData(saga.SourceAccount, message.SourceAccount));
            Assert.True(SagaInstanceHelper.HasSameData(saga.DestAccount, message.DestAccount));

            Assert.True(await _testHarness.Sent.Any<PostTransactionToDest>());
        }

        [Fact]
        public async Task Then_We_Request_PostToDest()
        {
            var message = new TransactionPostedToSource()
            {
                PostingId = Guid.NewGuid(),
                TransactionId = 45,
                IsContra = false,
                Amount = new MoneyInfo() { Currency = 1, Value = 100_000 },
                SourceAccount = new InternalContracts.AccountId()
                {
                    Id = 5,
                    SubjectId = 5,
                    AccountNumber = "acc 0",
                    AccountType = Data.AccountType.CustomerAccount,
                    Name = "cust"
                },
                DestAccount = new InternalContracts.AccountId()
                {
                    Id = 6,
                    SubjectId = 6,
                    AccountType = Data.AccountType.CustomerAccount,
                    AccountNumber = "acc 1",
                    Name = "cust"
                },
                Timestamp = DateTime.UtcNow,
                TransactionType = Data.TransactionType.CustomerSendMoney,
                SourceBalanceAfter = new MoneyInfo() { Currency = 1, Value = 50_000 },
                Charges = null
            };
            await SendToSaga(message);
            Assert.True(await _sagaHarness.Sagas.Any());
            Assert.True(await _sagaHarness.Consumed.Any());
            var saga = _sagaHarness.Sagas.Contains(message.PostingId);
            Assert.True(await _testHarness.Sent.Any<PostTransactionToDest>());
        }
    }
}
