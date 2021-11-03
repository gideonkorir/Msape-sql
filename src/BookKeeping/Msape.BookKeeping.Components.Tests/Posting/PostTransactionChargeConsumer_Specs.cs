using Microsoft.EntityFrameworkCore;
using Msape.BookKeeping.Components.Consumers.Posting;
using Msape.BookKeeping.Data.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Msape.BookKeeping.Components.Tests.Posting
{
    public class TransactionCharge_Is_Posted : ConsumerTest<PostTransactionChargeConsumer>
    {
        private readonly long id = DateTime.Now.Ticks;
        private readonly long chargeId = DateTime.Now.Ticks / 2;

        public TransactionCharge_Is_Posted(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override Task SeedContext(BookKeepingContext context)
        {
            var cust = context.Accounts.Add(new Data.Account(
                id: 1,
                partyId: 23,
                Data.AccountClass.Liability,
                Data.AccountType.CustomerAccount,
                new Data.Money(0, 100_000)
                ));
            context.Accounts.Add(new Data.Account(
                id: 2,
                partyId: 100,
                Data.AccountClass.Liability,
                Data.AccountType.AgentFloat,
                new Data.Money(0, 0)
                ));
            context.Accounts.Add(new Data.Account(
                id: 10,
                partyId: 10,
                Data.AccountClass.Equity,
                Data.AccountType.CustomerWithdrawalCharge,
                new Data.Money(0, 0)
                ));
            var src = new Data.TransactionAccountInfo()
            {
                AccountId = 1,
                AccountSubjectId = 1
            };
            var dest = new Data.TransactionAccountInfo()
            {
                AccountSubjectId = 2,
                AccountId = 2
            };
            var chrg = new Data.TransactionAccountInfo()
            {
                AccountSubjectId = 45,
                AccountId = 10
            };
            var tx = context.Transactions.Add(
                    new Data.Transaction(
                        id,
                        new Data.Money(0, 100),
                        Data.TransactionType.CustomerWithdrawal,
                        false,
                        DateTime.UtcNow,
                        src,
                        dest,
                        "notes",
                        new List<Data.Transaction>()
                        {
                            new Data.Transaction
                            (
                                chargeId,
                                new Data.Money(0, 10),
                                Data.TransactionType.TransactionCharge,
                                false,
                                DateTime.UtcNow,
                                src,
                                chrg,
                                "null",
                                null
                            )
                        }
                        )
                    );
            tx.Entity.PostToSource(cust.Entity);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Spec()
        {
            var message = new PostTransactionCharge()
            {
                PostingId = Guid.NewGuid(),
                TransactionId = id,
                ChargeId = chargeId,
                Timestamp = DateTime.UtcNow
            };
            await _testHarness.InputQueueSendEndpoint.Send(message).ConfigureAwait(false);
            Assert.True(await _consumerHarness.Consumed.Any<PostTransactionCharge>());
            Assert.True(await _testHarness.Published.Any<TransactionChargePosted>());

            //check state
            await WithContext(async context =>
            {
                var tx = await context.Transactions
                    .Include(c => c.Entries)
                    .SingleAsync(c => c.Id == message.ChargeId)
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
}
