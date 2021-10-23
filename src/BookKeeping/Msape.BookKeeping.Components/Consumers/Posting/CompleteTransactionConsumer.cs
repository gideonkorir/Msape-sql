using MassTransit;
using Microsoft.Azure.Cosmos;
using Msape.BookKeeping.Components.Infra;
using Msape.BookKeeping.Data;
using System;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public class CompleteTransactionConsumer : IConsumer<CompleteTransaction>
    {
        private readonly ICosmosAccount _cosmosAccount;

        public CompleteTransactionConsumer(ICosmosAccount cosmosAccount)
        {
            _cosmosAccount = cosmosAccount;
        }
        public async Task Consume(ConsumeContext<CompleteTransaction> context)
        {
            var transactionResponse = await _cosmosAccount.Transactions.ReadItemAsync<Transaction>(
                id: CosmosId.FromGuid(context.Message.Transaction.Id),
                partitionKey: new PartitionKey(context.Message.Transaction.PartitionKey),
                requestOptions: null,
                cancellationToken: context.CancellationToken
                )
                .ConfigureAwait(false);
            var transaction = transactionResponse.Resource;
            if(transaction.Status == TransactionStatus.Failed)
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

            LinkedTransactionInfo charge = null;
            TransactionLink link;
            if ((link = transaction.GetLink(TransactionLinkType.Charge)) != null)
            {
                charge = new LinkedTransactionInfo()
                {
                    Transaction = new DocumentRef<Guid>()
                    {
                        Id = link.Id,
                        PartitionKey = link.PartitionKey
                    },
                    Amount = link.Amount,
                    DestAccount = new InternalContracts.AccountId()
                    {
                        Id = link.CreditAccount.Id,
                        PartitionKey = link.CreditAccount.PartitionKey,
                        AccountNumber = link.CreditAccount.AccountNumber,
                        AccountType = link.CreditAccount.AccountType,
                        Name = link.CreditAccount.Name
                    },
                    TransactionType = link.TransactionType
                };
            }
            

            await context.Publish(new TransactionCompleted()
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
                ChargeInfo = charge
            }).ConfigureAwait(false);
        }
    }
}
