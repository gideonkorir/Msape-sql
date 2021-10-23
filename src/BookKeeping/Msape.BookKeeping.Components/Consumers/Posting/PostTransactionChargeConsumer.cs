using MassTransit;
using Microsoft.Azure.Cosmos;
using Msape.BookKeeping.Components.Infra;
using Msape.BookKeeping.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public class PostTransactionChargeConsumer : IConsumer<PostTransactionCharge>
    {
        private readonly ICosmosAccount _cosmosAccount;

        public PostTransactionChargeConsumer(ICosmosAccount cosmosAccount)
        {
            _cosmosAccount = cosmosAccount ?? throw new System.ArgumentNullException(nameof(cosmosAccount));
        }

        public async Task Consume(ConsumeContext<PostTransactionCharge> context)
        {
            //if we have credited the account then ignore
            var entry = await GetEntryAsync(context.Message.Transaction.Id, context.Message.CreditAccount.PartitionKey, context.CancellationToken)
                .ConfigureAwait(false);
            if (entry != null)
            {
                //publish the transaction credited
                await RespondPosted(context, entry).ConfigureAwait(false);
            }
            else
            {
                var account = await _cosmosAccount.Accounts.ReadItemAsync<Account>(
                    id: CosmosId.FromGuid(context.Message.CreditAccount.Id),
                    partitionKey: new PartitionKey(context.Message.CreditAccount.PartitionKey),
                    requestOptions: null,
                    cancellationToken: context.CancellationToken
                    )
                    .ConfigureAwait(false);
                var (canCredit, failReason) = account.Resource.CanReceive(context.Message.Amount.ToMoney(), context.Message.IsContra);
                if (!canCredit)
                {
                    throw new InvalidOperationException($"Can not credit charge to account [id={account.Resource.Id}, partitionKey={account.Resource.PartitionKey}]. Can credit failed with reason {failReason}");
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
                    }, context.Message.IsContra
                    );
                    var batch = _cosmosAccount.Accounts.CreateTransactionalBatch(new PartitionKey(context.Message.CreditAccount.PartitionKey));
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
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private static async Task RespondPosted(ConsumeContext<PostTransactionCharge> context, Entry entry)
        {
            await context.RespondAsync(new TransactionChargePosted()
            {
                Account = context.Message.CreditAccount,
                BalanceAfter = new MoneyInfo
                {
                    Value = entry.BalanceAfter.Value,
                    Currency = entry.BalanceAfter.Currency
                },
                Timestamp = entry.PostedDate,
                Transaction = context.Message.Transaction,
                Parent = context.Message.Parent
            }).ConfigureAwait(false);
        }
    }
}
