using MassTransit;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Msape.BookKeeping.Components.Infra;
using Msape.BookKeeping.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public class ReversePostToSourceConsumer : IConsumer<ReversePostTransactionToSource>
    {
        private readonly ICosmosAccount _cosmosAccount;
        private readonly ILogger<ReversePostToSourceConsumer> _logger;

        public ReversePostToSourceConsumer(ICosmosAccount cosmosAccount, ILogger<ReversePostToSourceConsumer> logger)
        {
            _cosmosAccount = cosmosAccount ?? throw new System.ArgumentNullException(nameof(cosmosAccount));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Consume(ConsumeContext<ReversePostTransactionToSource> context)
        {
            //if we have credited the account then ignore. we could proceed but then the last undo made us unable to undo again
            var entry = await GetEntryAsync(context.Message.Transaction.Id, context.Message.Account.PartitionKey, context.Message.IsContra, context.CancellationToken)
                .ConfigureAwait(false);
            if (entry != null)
            {
                //publish the transaction credited
                await RespondUndone(context, entry).ConfigureAwait(false);
            }
            else
            {
                var account = await _cosmosAccount.Accounts.ReadItemAsync<Account>(
                    id: CosmosId.FromGuid(context.Message.Transaction.Id),
                    partitionKey: new PartitionKey(context.Message.Account.PartitionKey),
                    requestOptions: null,
                    cancellationToken: context.CancellationToken
                    )
                    .ConfigureAwait(false);
                var total = HelperExtensions.GetTotal(context.Message.Amount, context.Message.Charge?.Amount);
                var (canReceive, failReason) = account.Resource.CanReceive(total, context.Message.IsContra);
                if (!canReceive)
                {
                    //loop until we succeed
                    if(_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning("Rescheduling undo transaction [id: {transactionId}, partKey: {partitionKey}] because of failure {failReason}",
                            context.Message.Transaction.Id,
                            context.Message.Transaction.PartitionKey,
                            failReason.ToString()
                            );
                    }
                    await context.ScheduleSend(TimeSpan.FromMinutes(5), context.Message).ConfigureAwait(false);
                }
                else
                {
                    var parentEntry = account.Resource.Receive(new DebitOrCreditInfo()
                    {
                        Amount = context.Message.Amount.ToMoney(),
                        MovementInfo = new EntryTransactionInfo(
                            Id: context.Message.Transaction.Id,
                            PartitionKey: context.Message.Transaction.PartitionKey,
                            Timestamp: context.Message.Timestamp,
                            TransactionType: context.Message.TransactionType
                            )
                    },
                    context.Message.IsContra);
                    Entry chargeEntry = null;
                    if(context.Message.Charge != null)
                    {
                        chargeEntry = account.Resource.Receive(new DebitOrCreditInfo()
                        {
                            Amount = context.Message.Charge.Amount.ToMoney(),
                            MovementInfo = new EntryTransactionInfo(
                            Id: context.Message.Charge.Transaction.Id,
                            PartitionKey: context.Message.Charge.Transaction.PartitionKey,
                            Timestamp: context.Message.Timestamp,
                            TransactionType: context.Message.Charge.TransactionType
                            )
                        },
                    context.Message.IsContra);
                    }
                    var batch = _cosmosAccount.Accounts.CreateTransactionalBatch(new PartitionKey(context.Message.Account.PartitionKey));
                    batch
                        .ReplaceItem(CosmosId.FromGuid(account.Resource.Id), account.Resource, new TransactionalBatchItemRequestOptions()
                        {
                            IfMatchEtag = account.ETag,
                            EnableContentResponseOnWrite = false
                        })
                        .CreateItem(parentEntry, new TransactionalBatchItemRequestOptions()
                        {
                            EnableContentResponseOnWrite = false
                        });
                    if(chargeEntry != null)
                    {
                        //insert
                        batch.CreateItem(chargeEntry, new TransactionalBatchItemRequestOptions() { EnableContentResponseOnWrite = true });
                    }
                    var response = await batch.ExecuteAsync(context.CancellationToken).ConfigureAwait(false);
                    response.ThrowIfNotSuccessful();
                    await RespondUndone(context, parentEntry).ConfigureAwait(false);
                }
            }
        }

        private async Task<Entry> GetEntryAsync(Guid transactionId, string partKey, bool isContra, CancellationToken cancellationToken)
        {
            var id = Entry.CreateId(transactionId, isContra ? EntryType.Debit : EntryType.Credit);
            try
            {
                var response = await _cosmosAccount.Accounts.ReadItemAsync<Entry>(
                    id: id,
                    partitionKey: new PartitionKey(partKey),
                    requestOptions: null,
                    cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private static async Task RespondUndone(ConsumeContext<ReversePostTransactionToSource> context, Entry entry)
        {
            await context.RespondAsync(new TransactionPostToSourceReversed()
            {
                BalanceAfter = new MoneyInfo
                {
                    Value = entry.BalanceAfter.Value,
                    Currency = entry.BalanceAfter.Currency
                },
                Timestamp = entry.PostedDate,
                Transaction = context.Message.Transaction
            }).ConfigureAwait(false);
        }
    }
}
