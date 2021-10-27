using MassTransit;
using Microsoft.EntityFrameworkCore;
using Msape.BookKeeping.Data;
using Msape.BookKeeping.Data.EF;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public class PostTransactionToDestConsumer : IConsumer<PostTransactionToDest>
    {
        private readonly BookKeepingContext _bookeepingContext;

        public PostTransactionToDestConsumer(BookKeepingContext bookeepingContext)
        {
            _bookeepingContext = bookeepingContext ?? throw new ArgumentNullException(nameof(bookeepingContext));
        }

        public async Task Consume(ConsumeContext<PostTransactionToDest> context)
        {
            var transaction = await _bookeepingContext.Transactions
                .Where(c => c.Id == context.Message.TransactionId)
                .SingleAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if(transaction.Status == TransactionStatus.Succeeded)
            {
                await RespondPosted(context, transaction).ConfigureAwait(false);
            }
            else
            {
                var account = await _bookeepingContext.Accounts
                    .FindAsync(new object[] { context.Message.DestAccountId }, context.CancellationToken)
                    .ConfigureAwait(false);
                if(transaction.Status == TransactionStatus.Initiated)
                {
                    var failReason = transaction.PostToDestination(account);
                    await _bookeepingContext.SaveChangesAsync(context.CancellationToken)
                        .ConfigureAwait(false);
                    var task = transaction.Status == TransactionStatus.Succeeded
                        ? RespondPosted(context, transaction)
                        : RespondPostFailed(context, account, failReason.Value);
                    await task.ConfigureAwait(false);
                }
                else if(transaction.Status == TransactionStatus.Failed
                    && transaction.DestFailReason.HasValue)
                {
                    await RespondPostFailed(context, account, transaction.DestFailReason.Value)
                        .ConfigureAwait(false);
                }
            }
        }

        private static async Task RespondPosted(ConsumeContext<PostTransactionToDest> context, Transaction transaction)
        {
            var entry = transaction.GetDestEntry();
            await context.RespondAsync(new TransactionPostedToDest()
            {
                PostingId = context.Message.PostingId,
                AccountId = context.Message.DestAccountId,
                BalanceAfter = new MoneyInfo
                {
                    Value = entry.BalanceAfter.Value,
                    Currency = entry.BalanceAfter.Currency
                },
                Timestamp = entry.PostedDate,
                TransactionId = context.Message.TransactionId
            }).ConfigureAwait(false);
        }

        private static async Task RespondPostFailed(ConsumeContext<PostTransactionToDest> context, Account account, DebitOrCreditFailReason failReason)
        {
            await context.RespondAsync(new PostTransactionToDestFailed()
            {
                PostingId = context.Message.PostingId,
                AccountId = context.Message.DestAccountId,
                AccountBalance = new MoneyInfo
                {
                    Value = account.Balance.Value,
                    Currency = account.Balance.Currency
                },
                FailReason = failReason,
                Timestamp = DateTime.UtcNow,
                TransactionId = context.Message.TransactionId
            }).ConfigureAwait(false);
        }
    }
}
