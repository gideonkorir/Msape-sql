using MassTransit;
using Microsoft.Azure.Cosmos;
using Msape.BookKeeping.Components.Infra;
using Msape.BookKeeping.InternalContracts;
using Msape.BookKeeping.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public class PostTransactionToSourceConsumer : IConsumer<PostTransaction>
    {
        protected readonly ICosmosAccount _cosmosAccount;

        public PostTransactionToSourceConsumer(ICosmosAccount cosmosAccount)
        {
            _cosmosAccount = cosmosAccount ?? throw new ArgumentNullException(nameof(cosmosAccount));
        }
        
        public async Task Consume(ConsumeContext<PostTransaction> context)
        {
            //save transaction
            var mappedInfo = await CreateTransaction(context).ConfigureAwait(false);
            var transaction = mappedInfo.Transaction;
            if(transaction.Status == TransactionStatus.Failed && transaction.FailReason == TransactionFailReason.FailedToInitiate)
            {
                //if it's already marked as failed then fail
                await PublishFailedToInitiate(context, mappedInfo).ConfigureAwait(false);
            }
            else if(transaction.Status == TransactionStatus.Initiated)
            {
                //we have work to do here

                //load account and debit
                var initResult = await InitiateTransaction(mappedInfo, context.CancellationToken).ConfigureAwait(false);

                var task = initResult.Status switch
                {
                    InitiateTransactionStatus.Initiated => HandleInitiateCompleted(context, mappedInfo, initResult),
                    InitiateTransactionStatus.Failed => HandleInitiateFailed(context, mappedInfo),
                    _ => Task.CompletedTask
                };
                await task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Called to created the transaction in the underlying database. If the transaction
        /// already exists then returns the existing transaction.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<MappedTransactionInfo> CreateTransaction(ConsumeContext<PostTransaction> context)
        {
            var mapResult = await MapToTransaction(context.Message, context.CancellationToken).ConfigureAwait(false);
            var tx = await Write(mapResult.Transaction, context.CancellationToken).ConfigureAwait(false);

            if(mapResult.HasCharge)
            {
                var charge = await Write(mapResult.Charge, context.CancellationToken).ConfigureAwait(false);
                mapResult = mapResult with { Transaction = tx, Charge = charge };
            }
            else
            {
                mapResult = mapResult with { Transaction = tx };
            }

            return mapResult;
            

            async Task<Transaction> Write(Transaction movement, CancellationToken cancellationToken)
            {
                try
                {
                    var response = await _cosmosAccount.Transactions.CreateItemAsync(
                        item: movement,
                        partitionKey: new PartitionKey(movement.PartitionKey),
                        requestOptions: null,
                        cancellationToken: cancellationToken
                        ).ConfigureAwait(false);
                    return response.Resource;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var response = await _cosmosAccount.Transactions.ReadItemAsync<Transaction>(
                        id: CosmosId.FromGuid(movement.Id),
                        partitionKey: new PartitionKey(movement.PartitionKey),
                        requestOptions: null,
                        cancellationToken: cancellationToken
                        ).ConfigureAwait(false);
                    return response.Resource;
                }
            }
        }

        /// <summary>
        /// Called to map the message value to the a transaction object
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual ValueTask<MappedTransactionInfo> MapToTransaction(PostTransaction message, CancellationToken cancellationToken)
        {
            var transaction = new Transaction(
                id: message.Id,
                partitionKey: CosmosId.FromGuid(message.Id),
                amount: new Money(message.Currency, message.Amount),
                transactionType: message.TransactionType,
                isContra: message.IsContra,
                timestamp: message.Timestamp,
                sourceAccount: new TransactionAccountInfo()
                {
                    Id = message.DebitAccountId.Id,
                    PartitionKey = message.DebitAccountId.PartitionKey,
                    Name = message.DebitAccountId.Name,
                    AccountNumber = message.DebitAccountId.AccountNumber,
                    AccountType = message.DebitAccountId.AccountType
                },
                destAccount: new TransactionAccountInfo()
                {
                    Id = message.CreditAccountId.Id,
                    PartitionKey = message.CreditAccountId.PartitionKey,
                    Name = message.CreditAccountId.Name,
                    AccountNumber = message.CreditAccountId.AccountNumber,
                    AccountType = message.CreditAccountId.AccountType
                },
                notes: $"Transaction {message.TransactionType} from {message.DebitAccountId.AccountNumber} to {message.CreditAccountId.AccountNumber}"
                );

            Transaction charge = null;
            if(message.Charge != null)
            {
                charge = new Transaction(
                    id: message.Charge.Id,
                    partitionKey: transaction.PartitionKey,
                    amount: new Money(message.Charge.Currency, message.Charge.Amount),
                    transactionType: message.Charge.TransactionType,
                    isContra: message.IsContra,
                    timestamp: message.Timestamp,
                    sourceAccount: transaction.SourceAccount,
                    destAccount: new TransactionAccountInfo()
                    {
                        Id = message.Charge.PayToAccount.Id,
                        PartitionKey = message.Charge.PayToAccount.PartitionKey,
                        Name = message.Charge.PayToAccount.Name,
                        AccountNumber = message.Charge.PayToAccount.AccountNumber,
                        AccountType = message.Charge.PayToAccount.AccountType
                    },
                    notes: "Transaction charges",
                    links: new List<TransactionLink>()
                    {
                        new TransactionLink
                        {
                            Id = transaction.Id,
                            PartitionKey = transaction.PartitionKey,
                            TransactionType = transaction.TransactionType,
                            Amount = transaction.Amount,
                            Timestamp = transaction.Timestamp,
                            LinkType = TransactionLinkType.Parent,
                            DebitAccount = transaction.SourceAccount,
                            CreditAccount = transaction.DestAccount
                        }
                    }
                    );
                transaction.Links.Add(new TransactionLink()
                {
                    Id = charge.Id,
                    PartitionKey = charge.PartitionKey,
                    Amount = charge.Amount,
                    TransactionType = charge.TransactionType,
                    Timestamp = charge.Timestamp,
                    LinkType = TransactionLinkType.Charge,
                    DebitAccount = charge.SourceAccount,
                    CreditAccount = charge.DestAccount
                });
            }
            var mappedInfo = new MappedTransactionInfo(transaction, charge);
            return new ValueTask<MappedTransactionInfo>(mappedInfo);
        }

        
        private async Task<InitiateTransactionResult> InitiateTransaction(MappedTransactionInfo mappedInfo, CancellationToken cancellationToken)
        {
            var transaction = mappedInfo.Transaction;
            var reference = transaction.SourceAccount;
            var entries = await FetchExistingEntries(transaction.Id, reference.PartitionKey, cancellationToken).ConfigureAwait(false);
            if (entries.Count == 2)
            {
                //we have both a credit and debit for the same transaction
                return new InitiateTransactionResult(InitiateTransactionStatus.Undone, new Money(0, 0), 0);
            }
            else if (entries.Count == 1)
            {
                //we have the debit
                return new InitiateTransactionResult(InitiateTransactionStatus.Initiated, entries[0].BalanceAfter);
            }
            else
            {
                // try create the entry
                var accountResponse = await _cosmosAccount.Accounts.ReadItemAsync<Account>
                    (
                    id: CosmosId.FromGuid(reference.Id),
                    partitionKey: new PartitionKey(reference.PartitionKey),
                    requestOptions: null,
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
                var account = accountResponse.Resource;
                var total = mappedInfo.Transaction.Amount + (mappedInfo.Charge?.Amount ?? Money.Zero(mappedInfo.Transaction.Amount));
                var (canInitiate, failReason) = account.CanInitiate(total, transaction.IsContra);
                if (!canInitiate)
                {
                    return new InitiateTransactionResult(InitiateTransactionStatus.Failed, account.Balance, failReason);
                }
                var batch = _cosmosAccount.Accounts.CreateTransactionalBatch(new PartitionKey(account.PartitionKey));

                //Start by doing the charge transaction if it's not null. The reason for that
                //is that we can then easily get the final transaction balance given the parent
                //transaction.
                //For statement since we don't need upto the millisecond time we can easily replace the
                //timestamp with that of the transaction i.e. we can have both an entry and statement timestamp
                if (mappedInfo.Charge != null)
                {
                    var chargeEntry = account.Initiate(new DebitOrCreditInfo()
                    {
                        MovementInfo = new EntryTransactionInfo(mappedInfo.Charge.Id, mappedInfo.Charge.PartitionKey, mappedInfo.Charge.Timestamp, mappedInfo.Charge.TransactionType),
                        Amount = mappedInfo.Charge.Amount
                    }, transaction.IsContra
                    );
                    batch.CreateItem(chargeEntry, new TransactionalBatchItemRequestOptions() { EnableContentResponseOnWrite = false });
                }

                var parentEntry = account.Initiate(new DebitOrCreditInfo()
                {
                    MovementInfo = new EntryTransactionInfo(transaction.Id, transaction.PartitionKey, transaction.Timestamp, transaction.TransactionType),
                    Amount = transaction.Amount
                }, transaction.IsContra
                );
                batch.CreateItem(parentEntry, new TransactionalBatchItemRequestOptions() { EnableContentResponseOnWrite = false });

                batch.ReplaceItem(CosmosId.FromGuid(account.Id), account, new TransactionalBatchItemRequestOptions()
                {
                    EnableContentResponseOnWrite = false,
                    IfMatchEtag = accountResponse.ETag
                });

                var response = await batch.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                response.ThrowIfNotSuccessful();

                return new InitiateTransactionResult(InitiateTransactionStatus.Initiated, account.Balance, null);
            }
        }

        private async Task<List<EntryInfo>> FetchExistingEntries(Guid transactionId, string partitionKey, CancellationToken cancellationToken)
        {
            var query = new QueryDefinition("SELECT c.id, c.balanceAfter, c.currency FROM c WHERE ARRAY_CONTAINS(@ids, c.id)")
                .WithParameter("@ids", new object[]
                {
                    Entry.CreateId(transactionId, EntryType.Credit),
                    Entry.CreateId(transactionId, EntryType.Debit)
                });
            var enumerator = _cosmosAccount.Transactions.GetItemQueryIterator<EntryInfo>(
                queryDefinition: query,
                continuationToken: null,
                requestOptions: new QueryRequestOptions()
                {
                    PartitionKey = new PartitionKey(partitionKey),
                    MaxItemCount = 2
                }
                );
            List<EntryInfo> values = new ();
            while(enumerator.HasMoreResults)
            {
                var results = await enumerator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                values.AddRange(results);
            }
            return values;
        }

        protected virtual async Task HandleInitiateCompleted(ConsumeContext<PostTransaction> context, MappedTransactionInfo mappedInfo, InitiateTransactionResult debitResult)
        {
            var transaction = mappedInfo.Transaction;
            var charge = mappedInfo.Charge;

            await context.Publish(new TransactionPostedToSource()
            {
                Amount = transaction.Amount,
                DestAccount = transaction.DestAccount.ToAccountId(),
                SourceAccount = transaction.SourceAccount.ToAccountId(),
                TransactionType = transaction.TransactionType,
                IsContra = transaction.IsContra,
                Timestamp = transaction.Timestamp,
                Transaction = new DocumentRef<Guid>()
                {
                    Id = transaction.Id,
                    PartitionKey = transaction.PartitionKey
                },
                ChargeInfo = charge == null ? null : new LinkedTransactionInfo()
                {
                    Amount = charge.Amount,
                    TransactionType = charge.TransactionType,
                    Transaction = new DocumentRef<Guid>()
                    {
                        Id = charge.Id,
                        PartitionKey = charge.PartitionKey
                    },
                    DestAccount = charge.DestAccount.ToAccountId()
                },
                SourceBalanceAfter = new MoneyInfo
                {
                    Value = debitResult.BalanceAfter.Value,
                    Currency = debitResult.BalanceAfter.Currency
                }
            }).ConfigureAwait(false);
        }

        protected virtual async Task HandleInitiateFailed(ConsumeContext<PostTransaction> context, MappedTransactionInfo mappedInfo)
        {
            //mark the transaction as failed and publish failed to debit
            var batch = _cosmosAccount.Transactions.CreateTransactionalBatch(new PartitionKey(mappedInfo.Transaction.PartitionKey));
            mappedInfo.Transaction.MarkFailed(TransactionFailReason.FailedToInitiate, DateTime.UtcNow);
            batch.ReplaceItem(
                id: CosmosId.FromGuid(mappedInfo.Transaction.Id),
                item: mappedInfo.Transaction,
                requestOptions: new TransactionalBatchItemRequestOptions()
                {
                    EnableContentResponseOnWrite = false,
                    IfMatchEtag = null
                }
                );
            if(mappedInfo.HasCharge)
            {
                mappedInfo.Charge.MarkFailed(TransactionFailReason.ParentTransactionFailed, DateTime.UtcNow);
                batch.ReplaceItem
                    (
                        id: CosmosId.FromGuid(mappedInfo.Charge.Id),
                        item: mappedInfo.Charge,
                        requestOptions: new TransactionalBatchItemRequestOptions()
                        {
                            EnableContentResponseOnWrite = false,
                            IfMatchEtag = null
                        }
                    );
            }

            var response = await batch.ExecuteAsync(cancellationToken: context.CancellationToken).ConfigureAwait(false);

            response.ThrowIfNotSuccessful();

            await PublishFailedToInitiate(context, mappedInfo).ConfigureAwait(false);
        }

        /// <summary>
        /// Not allowing override because I don't want the published event to change based on incoming event type
        /// that will create a variable topology that will make downstream consumers that much harder to build
        /// </summary>
        /// <param name="context"></param>
        /// <param name="mappedInfo"></param>
        /// <returns></returns>
        protected virtual async Task PublishFailedToInitiate(ConsumeContext<PostTransaction> context, MappedTransactionInfo mappedInfo)
        {
            await context.Publish(new TransactionFailed()
            {
                Transaction = new DocumentRef<Guid>()
                {
                    Id = mappedInfo.Transaction.Id,
                    PartitionKey = mappedInfo.Transaction.PartitionKey
                },
                Amount = mappedInfo.Transaction.Amount,
                Timestamp = mappedInfo.Transaction.Timestamp,
                FailReason = mappedInfo.Transaction.FailReason,
                TransactionType = mappedInfo.Transaction.TransactionType,
                SourceAccount = mappedInfo.Transaction.SourceAccount.ToAccountId(),
                DestAccount = mappedInfo.Transaction.DestAccount.ToAccountId(),
                IsContra = mappedInfo.Transaction.IsContra,
                CompletedDate = mappedInfo.Transaction.DateCompleted.Value,
                ChargeInfo = !mappedInfo.HasCharge ? null : new LinkedTransactionInfo()
                {
                    Amount = mappedInfo.Charge.Amount,
                    Transaction = new DocumentRef<Guid>()
                    {
                        Id = mappedInfo.Charge.Id,
                        PartitionKey = mappedInfo.Charge.PartitionKey
                    },
                    DestAccount = mappedInfo.Charge.DestAccount.ToAccountId(),
                    TransactionType = mappedInfo.Charge.TransactionType
                }
            }).ConfigureAwait(false);
        }
        
        protected record MappedTransactionInfo(Transaction Transaction, Transaction Charge)
        {
            public bool HasCharge => Charge != null;
        }

        protected enum InitiateTransactionStatus
        {
            Initiated,
            Failed,
            Undone
        }

        protected readonly struct InitiateTransactionResult
        {
            public InitiateTransactionStatus Status { get; }
            public Money BalanceAfter { get; }
            public DebitOrCreditFailReason? FailReason { get; }

            public InitiateTransactionResult(InitiateTransactionStatus status, Money balanceAfter, DebitOrCreditFailReason? failReason = null)
            {
                Status = status;
                BalanceAfter = balanceAfter;
                FailReason = failReason;
            }

            public void Deconstruct(out InitiateTransactionStatus status, out Money balanceAfter, out DebitOrCreditFailReason? failReason)
            {
                status = Status;
                balanceAfter = BalanceAfter;
                failReason = FailReason;
            }
        }

        private class EntryInfo
        {
            public string Id { get; set; }
            public Money BalanceAfter { get; set; }
        }

    }
}
