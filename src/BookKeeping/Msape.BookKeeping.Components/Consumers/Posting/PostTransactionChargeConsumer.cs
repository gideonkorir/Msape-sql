using MassTransit;
using Microsoft.EntityFrameworkCore;
using Msape.BookKeeping.Data;
using Msape.BookKeeping.Data.EF;
using System;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public class PostTransactionChargeConsumer : IConsumer<PostTransactionCharge>
    {
        private readonly BookKeepingContext _bookeepingContext;

        public PostTransactionChargeConsumer(BookKeepingContext bookeepingContext)
        {
            _bookeepingContext = bookeepingContext;
        }

        public async Task Consume(ConsumeContext<PostTransactionCharge> context)
        {
            //if we have credited the account then ignore
            var tx = await _bookeepingContext.Transactions
                .SingleAsync(c => c.Id == context.Message.ChargeId, context.CancellationToken)
                .ConfigureAwait(false);
            var account = await _bookeepingContext.Accounts
                    .SingleAsync(c => c.Id == tx.DestAccount.AccountId, context.CancellationToken)
                    .ConfigureAwait(false);
            var failReason = tx.PostToDestination(account);
            if (failReason.HasValue)
            {
                throw new InvalidOperationException($"Can not credit charge to account [id={account.Id}]. Can credit failed with reason {failReason}");
            }
            else
            {
                await _bookeepingContext.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
                await RespondPosted(context, tx).ConfigureAwait(false);
            }
        }

        private static async Task RespondPosted(ConsumeContext<PostTransactionCharge> context, Transaction tx)
        {
            var destEntry = tx.GetDestEntry();
            await context.RespondAsync(new TransactionChargePosted()
            {
                PostingId = context.Message.PostingId,
                BalanceAfter = new MoneyInfo
                {
                    Value = destEntry.BalanceAfter.Value,
                    Currency = destEntry.BalanceAfter.Currency
                },
                Timestamp = destEntry.PostedDate,
                ChargeId = context.Message.ChargeId,
                TransactionId = context.Message.TransactionId
            }).ConfigureAwait(false);
        }
    }
}
