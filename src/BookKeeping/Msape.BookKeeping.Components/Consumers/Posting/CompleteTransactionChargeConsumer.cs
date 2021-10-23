using MassTransit;
using Microsoft.Azure.Cosmos;
using Msape.BookKeeping.Components.Infra;
using Msape.BookKeeping.Data;
using System;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public class CompleteTransactionChargeConsumer : IConsumer<CompleteTransactionCharge>
    {
        private readonly ICosmosAccount _cosmosAccount;

        public CompleteTransactionChargeConsumer(ICosmosAccount cosmosAccount)
        {
            _cosmosAccount = cosmosAccount;
        }
        public async Task Consume(ConsumeContext<CompleteTransactionCharge> context)
        {
            var transactionResponse = await _cosmosAccount.Transactions.ReadItemAsync<Transaction>(
                id: CosmosId.FromGuid(context.Message.Transaction.Id),
                partitionKey: new PartitionKey(context.Message.Transaction.PartitionKey),
                requestOptions: null,
                cancellationToken: context.CancellationToken
                )
                .ConfigureAwait(false);
            var transaction = transactionResponse.Resource;
            if (transaction.Status == TransactionStatus.Failed)
            {
                throw new InvalidOperationException($"Failed to completed transaction [id: {transaction.Id}, partitionKey: {transaction.PartitionKey}] because it was in a failed state");
            }

            if (transaction.Status == TransactionStatus.Initiated)
            {
                transaction.Complete(
                    new CompleteTransactionData(
                        context.Message.Timestamp,
                        new Money(context.Message.DebitBalance.Currency, context.Message.DebitBalance.Value),
                        context.Message.DebitTimestamp,
                        new Money(context.Message.CreditBalance.Currency, context.Message.CreditBalance.Value),
                        context.Message.CreditTimestamp
                        )
                    );
                await _cosmosAccount.Transactions.ReplaceItemAsync(
                    transaction,
                    id: CosmosId.FromGuid(context.Message.Transaction.Id),
                    partitionKey: new PartitionKey(transaction.PartitionKey),
                    requestOptions: new ItemRequestOptions()
                    {
                        IfMatchEtag = transactionResponse.ETag,
                        EnableContentResponseOnWrite = false
                    },
                    cancellationToken: context.CancellationToken
                    ).ConfigureAwait(false);
            }
            var parent = transaction.GetLink(TransactionLinkType.Parent);
            await context.Publish(new TransactionChargeCompleted()
            {
                Transaction = context.Message.Transaction,
                Timestamp = context.Message.Timestamp,
                TransactionType = transaction.TransactionType,
                IsContra = transaction.IsContra,
                SourceAccount = transaction.SourceAccount.ToAccountId(),
                DestAccount = transaction.DestAccount.ToAccountId(),
                SourceBalance = transaction.CompleteTransactionData.SourceBalance,
                DestBalance = transaction.CompleteTransactionData.DestBalance,
                Amount = transaction.Amount,
                Parent = new LinkedTransactionInfo()
                {
                    Amount = parent.Amount,
                    DestAccount = parent.CreditAccount.ToAccountId(),
                    Transaction = new DocumentRef<Guid>()
                    {
                        Id = parent.Id,
                        PartitionKey = parent.PartitionKey
                    },
                    TransactionType = parent.TransactionType
                }
            }).ConfigureAwait(false);
        }
    }
}
