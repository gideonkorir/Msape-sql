using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Msape.BookKeeping.Components.Consumers.Posting;
using Msape.BookKeeping.Data.EF;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Msape.BookKeeping.Components.Tests.Posting
{
    public class Transaction_Is_Posted_To_Dest : ConsumerTest<PostTransactionToDestConsumer>
    {
        private readonly long id = DateTime.Now.Ticks;

        public Transaction_Is_Posted_To_Dest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override Task SeedContext(BookKeepingContext context)
        {
            var src = context.Accounts.Add(new Data.Account(
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
            var tx = context.Transactions.Add(
                    new Data.Transaction(
                        id,
                        "receipt",
                        new Data.Money(0, 100),
                        Data.TransactionType.AgentFloatTopup,
                        false,
                        DateTime.UtcNow,
                        new Data.TransactionAccountInfo()
                        {
                            AccountId = 1,
                            AccountSubjectId = 1
                        },
                        new Data.TransactionAccountInfo()
                        {
                            AccountSubjectId = 2,
                            AccountId = 2
                        },
                        "notes",
                        null
                        )
                    );
            tx.Entity.PostToSource(src.Entity);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Spec()
        {
            var message = new PostTransactionToDest()
            {
                PostingId = Guid.NewGuid(),
                TransactionId = id,
                Timestamp = DateTime.UtcNow
            };
            await _testHarness.InputQueueSendEndpoint.Send(message).ConfigureAwait(false);
            Assert.True(await _consumerHarness.Consumed.Any<PostTransactionToDest>());
            Assert.True(await _testHarness.Published.Any<TransactionPostedToDest>());

            //check state
            await WithContext(async context =>
            {
                var tx = await context.Transactions
                    .Include(c => c.Entries)
                    .SingleAsync(c => c.Id == message.TransactionId)
                    .ConfigureAwait(false);
                Assert.NotNull(tx);
                Assert.Equal(Data.TransactionStatus.Succeeded, tx.Status);
                Assert.Equal(2, tx.Entries.Count);

                var acc1 = context.Accounts.Find(tx.DestAccount.AccountId);
                Assert.NotNull(acc1);
                Assert.Equal(tx.Amount with { }, acc1.Balance);
            });
        }
    }

    public class Post_To_Dest_Is_Idempotent : ConsumerTest<PostTransactionToDestConsumer>
    {
        private readonly long id = DateTime.Now.Ticks;

        public Post_To_Dest_Is_Idempotent(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override Task SeedContext(BookKeepingContext context)
        {
            var src = context.Accounts.Add(new Data.Account(
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
            var tx = context.Transactions.Add(
                    new Data.Transaction(
                        id,
                        "receit",
                        new Data.Money(0, 100),
                        Data.TransactionType.AgentFloatTopup,
                        false,
                        DateTime.UtcNow,
                        new Data.TransactionAccountInfo()
                        {
                            AccountId = 1,
                            AccountSubjectId = 1
                        },
                        new Data.TransactionAccountInfo()
                        {
                            AccountSubjectId = 2,
                            AccountId = 2
                        },
                        "notes",
                        null
                        )
                    );
            tx.Entity.PostToSource(src.Entity);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Spec()
        {
            var message = new PostTransactionToDest()
            {
                PostingId = Guid.NewGuid(),
                TransactionId = id,
                Timestamp = DateTime.UtcNow
            };
            await _testHarness.InputQueueSendEndpoint.Send(message).ConfigureAwait(false);
            Assert.True(await _consumerHarness.Consumed.Any<PostTransactionToDest>());
            Assert.True(await _testHarness.Published.Any<TransactionPostedToDest>());

            //check state
            await WithContext(async context =>
            {
                var tx = await context.Transactions
                    .Include(c => c.Entries)
                    .SingleAsync(c => c.Id == message.TransactionId)
                    .ConfigureAwait(false);
                Assert.NotNull(tx);
                Assert.Equal(Data.TransactionStatus.Succeeded, tx.Status);
                Assert.Equal(2, tx.Entries.Count);

                var acc1 = context.Accounts.Find(tx.DestAccount.AccountId);
                Assert.NotNull(acc1);
                Assert.Equal(tx.Amount with { }, acc1.Balance);
            });

            await _testHarness.InputQueueSendEndpoint.Send(message).ConfigureAwait(false);
            Assert.Equal(2, _consumerHarness.Consumed.Count());
            Assert.Equal(2, _testHarness.Published.Count());

            //check state
            await WithContext(async context =>
            {
                var tx = await context.Transactions
                    .Include(c => c.Entries)
                    .SingleAsync(c => c.Id == message.TransactionId)
                    .ConfigureAwait(false);
                Assert.NotNull(tx);
                Assert.Equal(Data.TransactionStatus.Succeeded, tx.Status);
                Assert.Equal(2, tx.Entries.Count);

                var acc1 = context.Accounts.Find(tx.DestAccount.AccountId);
                Assert.NotNull(acc1);
                Assert.Equal(tx.Amount with { }, acc1.Balance);
            });
        }
    }

    public class Post_To_Dest_Failure_Scenario : ConsumerTest<PostTransactionToDestConsumer>
    {
        private readonly long id = DateTime.Now.Ticks;

        public Post_To_Dest_Failure_Scenario(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override Task SeedContext(BookKeepingContext context)
        {
            var src = context.Accounts.Add(new Data.Account(
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
                new Data.Money(0, 0),
                maxBalance: 50
                ));
            var tx = context.Transactions.Add(
                    new Data.Transaction(
                        id,
                        "receipt",
                        new Data.Money(0, 100),
                        Data.TransactionType.AgentFloatTopup,
                        false,
                        DateTime.UtcNow,
                        new Data.TransactionAccountInfo()
                        {
                            AccountId = 1,
                            AccountSubjectId = 1
                        },
                        new Data.TransactionAccountInfo()
                        {
                            AccountSubjectId = 2,
                            AccountId = 2
                        },
                        "notes",
                        null
                        )
                    );
            tx.Entity.PostToSource(src.Entity);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Spec()
        {
            var message = new PostTransactionToDest()
            {
                PostingId = Guid.NewGuid(),
                TransactionId = id,
                Timestamp = DateTime.UtcNow
            };
            await _testHarness.InputQueueSendEndpoint.Send(message).ConfigureAwait(false);
            Assert.True(await _consumerHarness.Consumed.Any<PostTransactionToDest>());
            Assert.True(await _testHarness.Published.Any<PostTransactionToDestFailed>());

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

                var acc1 = context.Accounts.Find(tx.DestAccount.AccountId);
                Assert.NotNull(acc1);
                Assert.Equal(0, acc1.Balance.Value);
            });
        }
    }
}
