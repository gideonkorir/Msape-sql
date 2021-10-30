using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Msape.BookKeeping.Components.Consumers.Posting;
using Msape.BookKeeping.Data.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Msape.BookKeeping.Components.Tests.Posting
{
    public class Consumer_Will_Cancel_Transaction : ConsumerTest<CancelTransactionConsumer>
    {
        private readonly long id = DateTime.Now.Ticks;

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
            var message = new CancelTransaction()
            {
                PostingId = Guid.NewGuid(),
                TransactionId = id,
                Timestamp = DateTime.UtcNow
            };
            await _testHarness.InputQueueSendEndpoint.Send(message).ConfigureAwait(false);
            Assert.True(await _consumerHarness.Consumed.Any<CancelTransaction>());
            Assert.True(await _testHarness.Published.Any<TransactionCancelled>());

            //check state
            await WithContext(async context =>
            {
                var tx = await context.Transactions
                    .Include(c => c.Entries)
                    .SingleAsync(c => c.Id == message.TransactionId)
                    .ConfigureAwait(false);
                Assert.NotNull(tx);
                Assert.Equal(Data.TransactionStatus.Cancelled, tx.Status);
                Assert.Equal(2, tx.Entries.Count);

                Assert.True(tx.Entries.All(c => c.AccountId == tx.SourceAccount.AccountId));

                var acc1 = context.Accounts.Find(tx.SourceAccount.AccountId);
                Assert.NotNull(acc1);
                Assert.Equal(0, acc1.Balance.Value);
            });
        }
    }
}
