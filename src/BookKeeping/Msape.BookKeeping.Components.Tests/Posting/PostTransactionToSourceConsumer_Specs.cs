using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Msape.BookKeeping.Components.Consumers.Posting;
using Msape.BookKeeping.Data.EF;
using Msape.BookKeeping.InternalContracts;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Msape.BookKeeping.Components.Tests.Posting
{
    public class Consumer_Posts_Transaction_To_Source : ConsumerTest<PostTransactionToSourceConsumer>
    {
        private readonly long id = DateTime.Now.Ticks;

        protected override Task SeedContext(BookKeepingContext context)
        {
            context.Accounts.Add(new Data.Account(
                id: 1,
                partyId: 23,
                Data.AccountClass.Asset,
                Data.AccountType.SystemAgentFloat,
                new Data.Money(0, 0)
                ));
            context.Accounts.Add(new Data.Account(
                id: 2,
                partyId: 100,
                Data.AccountClass.Liability,
                Data.AccountType.AgentFloat,
                new Data.Money(0, 0)
                ));
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Spec()
        {
            var message = new PostTransaction()
            {
                PostingId = Guid.NewGuid(),
                TransactionId = id,
                Timestamp = DateTime.UtcNow,
                Amount = 100,
                IsContra = false,
                Currency = 0,
                TransactionType = Data.TransactionType.AgentFloatTopup,
                Charge = null,
                SourceAccount = new AccountId()
                {
                    Id = 1,
                    SubjectId = 1,
                    AccountNumber = "SYSTEM",
                    AccountType = Data.AccountType.SystemAgentFloat,
                    Name = "SYSTEM"
                },
                DestAccount = new AccountId()
                {
                    Id = 2,
                    AccountNumber = "AGENT",
                    AccountType = Data.AccountType.AgentFloat,
                    Name = "AGENT",
                    SubjectId = 2
                }
            };
            await _testHarness.InputQueueSendEndpoint.Send(message).ConfigureAwait(false);
            Assert.True(await _consumerHarness.Consumed.Any<PostTransaction>());
            Assert.True(await _testHarness.Published.Any<TransactionPostedToSource>());

            //check state
            await WithContext(async context =>
            {
                var tx = await context.Transactions
                    .Include(c => c.Entries)
                    .SingleAsync(c => c.Id == message.TransactionId)
                    .ConfigureAwait(false);
                Assert.NotNull(tx);
                Assert.Equal(Data.TransactionStatus.Initiated, tx.Status);
                Assert.Single(tx.Entries);

                var acc1 = context.Accounts.Find(message.SourceAccount.Id);
                Assert.NotNull(acc1);
                Assert.Equal(tx.Amount with { }, acc1.Balance);

                var acc2 = context.Accounts.Find(message.DestAccount.Id);
                Assert.NotNull(acc2);
                Assert.Equal(0, acc2.Balance.Value);
            });
        }
    }

    public class Consumer_Fails_To_Post_On_Insufficient_Balance : ConsumerTest<PostTransactionToSourceConsumer>
    {
        private readonly long id = DateTime.Now.Ticks;

        protected override Task SeedContext(BookKeepingContext context)
        {
            context.Accounts.Add(new Data.Account(
                id: 1,
                partyId: 23,
                Data.AccountClass.Liability,
                Data.AccountType.AgentFloat,
                new Data.Money(0, 0)
                ));
            context.Accounts.Add(new Data.Account(
                id: 2,
                partyId: 100,
                Data.AccountClass.Liability,
                Data.AccountType.AgentFloat,
                new Data.Money(0, 0)
                ));
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Spec()
        {
            var message = new PostTransaction()
            {
                PostingId = Guid.NewGuid(),
                TransactionId = id,
                Timestamp = DateTime.UtcNow,
                Amount = 100,
                IsContra = false,
                Currency = 0,
                TransactionType = Data.TransactionType.AgentFloatTopup,
                Charge = null,
                SourceAccount = new AccountId()
                {
                    Id = 1,
                    SubjectId = 1,
                    AccountNumber = "AGENT 0",
                    AccountType = Data.AccountType.AgentFloat,
                    Name = "AGENT 0"
                },
                DestAccount = new AccountId()
                {
                    Id = 2,
                    AccountNumber = "AGENT 1",
                    AccountType = Data.AccountType.AgentFloat,
                    Name = "AGENT 1",
                    SubjectId = 2
                }
            };
            await _testHarness.InputQueueSendEndpoint.Send(message).ConfigureAwait(false);
            Assert.True(await _consumerHarness.Consumed.Any<PostTransaction>());
            Assert.True(await _testHarness.Published.Any<TransactionFailed>());

            //check state
            await WithContext(async context =>
            {
                var tx = await context.Transactions
                    .Include(c => c.Entries)
                    .SingleAsync(c => c.Id == message.TransactionId)
                    .ConfigureAwait(false);
                Assert.NotNull(tx);
                Assert.Equal(Data.TransactionStatus.Failed, tx.Status);
                Assert.Empty(tx.Entries);

                var acc1 = context.Accounts.Find(message.SourceAccount.Id);
                Assert.NotNull(acc1);
                Assert.Equal(0, acc1.Balance.Value);

                var acc2 = context.Accounts.Find(message.DestAccount.Id);
                Assert.NotNull(acc2);
                Assert.Equal(0, acc2.Balance.Value);
            });
        }
    }

    public class Consumer_Is_Idempotent : ConsumerTest<PostTransactionToSourceConsumer>
    {
        private readonly long id = DateTime.Now.Ticks;

        protected override Task SeedContext(BookKeepingContext context)
        {
            context.Accounts.Add(new Data.Account(
                id: 1,
                partyId: 23,
                Data.AccountClass.Asset,
                Data.AccountType.SystemAgentFloat,
                new Data.Money(0, 0)
                ));
            context.Accounts.Add(new Data.Account(
                id: 2,
                partyId: 100,
                Data.AccountClass.Liability,
                Data.AccountType.AgentFloat,
                new Data.Money(0, 0)
                ));
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Spec()
        {
            var message = new PostTransaction()
            {
                PostingId = Guid.NewGuid(),
                TransactionId = id,
                Timestamp = DateTime.UtcNow,
                Amount = 100,
                IsContra = false,
                Currency = 0,
                TransactionType = Data.TransactionType.AgentFloatTopup,
                Charge = null,
                SourceAccount = new AccountId()
                {
                    Id = 1,
                    SubjectId = 1,
                    AccountNumber = "SYSTEM",
                    AccountType = Data.AccountType.SystemAgentFloat,
                    Name = "SYSTEM"
                },
                DestAccount = new AccountId()
                {
                    Id = 2,
                    AccountNumber = "AGENT",
                    AccountType = Data.AccountType.AgentFloat,
                    Name = "AGENT",
                    SubjectId = 2
                }
            };
            await _testHarness.InputQueueSendEndpoint.Send(message).ConfigureAwait(false);
            Assert.True(await _consumerHarness.Consumed.Any<PostTransaction>());
            Assert.True(await _testHarness.Published.Any<TransactionPostedToSource>());

            //check state
            await WithContext(async context =>
            {
                var tx = await context.Transactions
                    .Include(c => c.Entries)
                    .SingleAsync(c => c.Id == message.TransactionId)
                    .ConfigureAwait(false);
                Assert.NotNull(tx);
                Assert.Equal(Data.TransactionStatus.Initiated, tx.Status);
                Assert.Single(tx.Entries);

                var acc1 = context.Accounts.Find(message.SourceAccount.Id);
                Assert.NotNull(acc1);
                Assert.Equal(tx.Amount with { }, acc1.Balance);

                var acc2 = context.Accounts.Find(message.DestAccount.Id);
                Assert.NotNull(acc2);
                Assert.Equal(0, acc2.Balance.Value);
            });

            //push same message again
            await _testHarness.InputQueueSendEndpoint.Send(message).ConfigureAwait(false);
            Assert.Equal(2, _consumerHarness.Consumed.Count());
            Assert.Equal(2, _testHarness.Published.Count());

            await WithContext(async context =>
            {
                var tx = await context.Transactions
                    .Include(c => c.Entries)
                    .SingleAsync(c => c.Id == message.TransactionId)
                    .ConfigureAwait(false);
                Assert.NotNull(tx);
                Assert.Equal(Data.TransactionStatus.Initiated, tx.Status);
                Assert.Single(tx.Entries);

                var acc1 = context.Accounts.Find(message.SourceAccount.Id);
                Assert.NotNull(acc1);
                Assert.Equal(tx.Amount with { }, acc1.Balance);

                var acc2 = context.Accounts.Find(message.DestAccount.Id);
                Assert.NotNull(acc2);
                Assert.Equal(0, acc2.Balance.Value);
            });
        }
    }
}
