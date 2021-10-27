using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Msape.BookKeeping.Data;
using Msape.BookKeeping.Data.EF;
using System;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public class ReversePostToSourceConsumer : IConsumer<ReversePostTransactionToSource>
    {
        private readonly BookKeepingContext _bookKeepingContext;
        private readonly ILogger<ReversePostToSourceConsumer> _logger;

        public ReversePostToSourceConsumer(BookKeepingContext bookKeepingContext, ILogger<ReversePostToSourceConsumer> logger)
        {
            _bookKeepingContext = bookKeepingContext ?? throw new ArgumentNullException(nameof(bookKeepingContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Consume(ConsumeContext<ReversePostTransactionToSource> context)
        {
            //if we have credited the account then ignore. we could proceed but then the last undo made us unable to undo again
            var transaction = await _bookKeepingContext.Transactions
                .SingleAsync(c => c.Id == context.Message.TransactionId, context.CancellationToken)
                .ConfigureAwait(false);
            var account = await _bookKeepingContext.Accounts
                .FindAsync(new object[] { transaction.SourceAccount.AccountId }, context.CancellationToken)
                .ConfigureAwait(false);
            if (transaction.Status == TransactionStatus.Failed)
            {
                //publish message
            }
            else
            {
                var failReason = transaction.ReversePostToSource(account);
                if (failReason.HasValue)
                {
                    //loop until we succeed
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning("Rescheduling undo transaction [id: {transactionId}] because of failure {failReason}",
                            context.Message.TransactionId,
                            failReason.ToString()
                            );
                    }
                    await context.ScheduleSend(TimeSpan.FromMinutes(5), context.Message).ConfigureAwait(false);
                }
                else
                {
                    await _bookKeepingContext.SaveChangesAsync(context.CancellationToken)
                        .ConfigureAwait(false);
                    await RespondUndone(context, transaction).ConfigureAwait(false);
                }
            }
        }

        private static async Task RespondUndone(ConsumeContext<ReversePostTransactionToSource> context, Transaction transaction)
        {
            var entry = transaction.GetDestEntry();
            await context.RespondAsync(new TransactionPostToSourceReversed()
            {
                PostingId = context.Message.PostingId,
                BalanceAfter = new MoneyInfo
                {
                    Value = entry.BalanceAfter.Value,
                    Currency = entry.BalanceAfter.Currency
                },
                Timestamp = entry.PostedDate,
                TransactionId = context.Message.TransactionId
            }).ConfigureAwait(false);
        }
    }
}
