using MassTransit;
using Microsoft.EntityFrameworkCore;
using Msape.BookKeeping.Data;
using Msape.BookKeeping.Data.EF;
using Msape.BookKeeping.InternalContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public class PostTransactionToSourceConsumer : IConsumer<PostTransaction>
    {
        private readonly BookKeepingContext _bookeepingContext;

        public PostTransactionToSourceConsumer(BookKeepingContext bookeepingContext)
        {
            _bookeepingContext = bookeepingContext ?? throw new ArgumentNullException(nameof(bookeepingContext));
        }
        
        public async Task Consume(ConsumeContext<PostTransaction> context)
        {
            //save transaction
            var transaction = await PostTransactionAsync(context).ConfigureAwait(false);
            if(transaction.Status == TransactionStatus.Failed 
                && transaction.FailReason == TransactionFailReason.FailedToPostToSource)
            {
                //if it's already marked as failed then fail
                await PublishFailedToInitiate(context, transaction).ConfigureAwait(false);
            }
            else if(transaction.Status == TransactionStatus.Initiated)
            {
                await HandleInitiateCompleted(context, transaction).ConfigureAwait(false);
            }
        }

        private async Task<Transaction> PostTransactionAsync(ConsumeContext<PostTransaction> context)
        {
            var existingTx = await FindTransaction(context.Message.TransactionId, context.CancellationToken).ConfigureAwait(false);
            if(existingTx != null)
            {
                return existingTx;
            }
            var transaction = MapToTransaction(context.Message, context.CancellationToken);
            _bookeepingContext.Transactions.Add(transaction);
            var reference = transaction.SourceAccount;
            // try create the entry
            var account = await _bookeepingContext.Accounts
                .Where(c => c.Id == reference.AccountId)
                .SingleAsync(context.CancellationToken)
                .ConfigureAwait(false);
            var failReason = transaction.PostToSource(account);

            await _bookeepingContext.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            return transaction;
        }

        /// <summary>
        /// Called to map the message value to the a transaction object
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual Transaction MapToTransaction(PostTransaction message, CancellationToken cancellationToken)
        {
            List<Transaction> charges = null;
            if (message.Charges != null)
            {
                charges = message.Charges.ConvertAll(charge => new Transaction(
                    id: charge.Id,
                    receiptNumber: charge.ReceiptNumber,
                    amount: new Money(charge.Currency, charge.Amount),
                    transactionType: charge.TransactionType,
                    isContra: message.IsContra,
                    timestamp: message.Timestamp,
                    sourceAccount: new TransactionAccountInfo()
                    {
                        AccountId = message.SourceAccount.Id,
                        AccountSubjectId = message.SourceAccount.SubjectId
                    },
                    destAccount: new TransactionAccountInfo()
                    {
                        AccountId = charge.PayToAccount.Id,
                        AccountSubjectId = charge.PayToAccount.SubjectId
                    },
                    notes: "Transaction charges",
                    charges: null
                    )
                );
            }
            var transaction = new Transaction(
                id: message.TransactionId,
                receiptNumber: message.ReceiptNumber,
                amount: new Money(message.Currency, message.Amount),
                transactionType: message.TransactionType,
                isContra: message.IsContra,
                timestamp: message.Timestamp,
                sourceAccount: new TransactionAccountInfo()
                {
                    AccountId = message.SourceAccount.Id,
                    AccountSubjectId = message.SourceAccount.SubjectId
                },
                destAccount: new TransactionAccountInfo()
                {
                    AccountId = message.DestAccount.Id,
                    AccountSubjectId = message.DestAccount.SubjectId
                },
                notes: $"Transaction {message.TransactionType} from {message.SourceAccount.AccountNumber} to {message.DestAccount.AccountNumber}",
                charges: charges
                );


            return transaction;
        }  

        private async Task<Transaction> FindTransaction(ulong transactionId, CancellationToken cancellationToken)
        {
            var transaction = await _bookeepingContext.Transactions
                .Where(c => c.Id == transactionId)
                .Include(c => c.Charges)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            return transaction;
        }

        protected virtual async Task HandleInitiateCompleted(ConsumeContext<PostTransaction> context, Transaction transaction)
        {
            var charges = transaction.Charges;
            var entry = transaction.GetSourceEntry();

            await context.Publish(new TransactionPostedToSource()
            {
                PostingId = context.Message.PostingId,
                Amount = transaction.Amount,
                DestAccount = context.Message.DestAccount,
                SourceAccount = context.Message.SourceAccount,
                TransactionType = transaction.TransactionType,
                IsContra = transaction.IsContra,
                Timestamp = transaction.Timestamp,
                TransactionId = transaction.Id,
                Charges = charges.ConvertAll(charge => new LinkedTransactionInfo()
                {
                    Amount = charge.Amount,
                    TransactionType = charge.TransactionType,
                    TransactionId = charge.Id,
                    DestAccount = context.Message.Charges.Find(c => c.Id == charge.Id).PayToAccount
                }),
                SourceBalanceAfter = new MoneyInfo
                {
                    Value = entry.BalanceAfter.Value,
                    Currency = entry.BalanceAfter.Currency
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Not allowing override because I don't want the published event to change based on incoming event type
        /// that will create a variable topology that will make downstream consumers that much harder to build
        /// </summary>
        /// <param name="context"></param>
        /// <param name="mappedInfo"></param>
        /// <returns></returns>
        protected virtual async Task PublishFailedToInitiate(ConsumeContext<PostTransaction> context, Transaction transaction)
        {
            await context.Publish(new TransactionFailed()
            {
                PostingId = context.Message.PostingId,
                TransactionId = transaction.Id,
                Amount = transaction.Amount,
                Timestamp = transaction.Timestamp,
                FailReason = transaction.FailReason,
                TransactionType = transaction.TransactionType,
                SourceAccount = transaction.SourceAccount,
                DestAccount = transaction.DestAccount,
                IsContra = transaction.IsContra,
                CompletedDate = transaction.DateCompleted.Value,
                ChargeInfo = transaction.Charges.ConvertAll(c => new LinkedTransactionInfo()
                {
                    Amount = c.Amount,
                    TransactionId = c.Id,
                    DestAccount = context.Message.Charges.Find(c => c.Id == c.Id).PayToAccount,
                    TransactionType = c.TransactionType
                })
            }).ConfigureAwait(false);
        }

    }
}
