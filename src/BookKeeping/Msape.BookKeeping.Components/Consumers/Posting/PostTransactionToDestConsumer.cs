using MassTransit;
using Microsoft.Azure.Cosmos;
using Msape.BookKeeping.Components.Infra;
using Msape.BookKeeping.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public class PostTransactionToDestConsumer : IConsumer<PostTransactionToDest>
    {
        private readonly ICosmosAccount _cosmosAccount;

        public PostTransactionToDestConsumer(ICosmosAccount cosmosAccount)
        {
            _cosmosAccount = cosmosAccount ?? throw new System.ArgumentNullException(nameof(cosmosAccount));
        }

        public async Task Consume(ConsumeContext<PostTransactionToDest> context)
        {
            //if we have credited the account then ignore
            var entry = await GetEntryAsync(context.Message.Transaction.Id, context.Message.DestAccount.PartitionKey, context.CancellationToken)
                .ConfigureAwait(false);
            if(entry != null)
            {
                //publish the transaction credited
                await RespondPosted(context, entry).ConfigureAwait(false);
            }
            else
            {
                var account = await _cosmosAccount.Accounts.ReadItemAsync<Account>(
                    id: CosmosId.FromGuid(context.Message.DestAccount.Id),
                    partitionKey: new PartitionKey(context.Message.DestAccount.PartitionKey),
                    requestOptions: null,
                    cancellationToken: context.CancellationToken
                    )
                    .ConfigureAwait(false);
                var (canReceive, failReason) = account.Resource.CanReceive(context.Message.Amount.ToMoney(), context.Message.IsContra);
                if(!canReceive)
                {
                    await RespondPostFailed(context, account.Resource, failReason).ConfigureAwait(false);
                }
                else
                {
                    var creditResult = account.Resource.Receive(new DebitOrCreditInfo()
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
                    var batch = _cosmosAccount.Accounts.CreateTransactionalBatch(new PartitionKey(context.Message.DestAccount.PartitionKey));
                    batch
                        .ReplaceItem(CosmosId.FromGuid(account.Resource.Id), account.Resource, new TransactionalBatchItemRequestOptions()
                        {
                            IfMatchEtag = account.ETag,
                            EnableContentResponseOnWrite = false
                        })
                        .CreateItem(creditResult, new TransactionalBatchItemRequestOptions()
                        {
                            EnableContentResponseOnWrite = false
                        });
                    var response = await batch.ExecuteAsync(context.CancellationToken).ConfigureAwait(false);
                    response.ThrowIfNotSuccessful();
                    await RespondPosted(context, creditResult).ConfigureAwait(false);
                }
            }
        }

        private async Task<Entry> GetEntryAsync(Guid transactionId, string partKey, CancellationToken cancellationToken)
        {
            var id = Entry.CreateId(transactionId, EntryType.Credit);
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
            catch(CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private static async Task RespondPosted(ConsumeContext<PostTransactionToDest> context, Entry entry)
        {
            await context.RespondAsync(new TransactionPostedToDest()
            {
                Account = context.Message.DestAccount,
                BalanceAfter = new MoneyInfo
                {
                    Value = entry.BalanceAfter.Value,
                    Currency = entry.BalanceAfter.Currency
                },
                Timestamp = entry.PostedDate,
                Transaction = context.Message.Transaction
            }).ConfigureAwait(false);
        }

        private static async Task RespondPostFailed(ConsumeContext<PostTransactionToDest> context, Account account, DebitOrCreditFailReason failReason)
        {
            await context.RespondAsync(new PostTransactionToDestFailed()
            {
                Account = context.Message.DestAccount,
                AccountBalance = new MoneyInfo
                {
                    Value = account.Balance.Value,
                    Currency = account.Balance.Currency
                },
                FailReason = failReason,
                Timestamp = DateTime.UtcNow,
                Transaction = context.Message.Transaction
            }).ConfigureAwait(false);
        }
    }
}
