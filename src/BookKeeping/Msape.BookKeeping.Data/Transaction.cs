using Msape.BookKeeping.Data;
using System;
using System.Collections.Generic;

namespace Msape.BookKeeping.Data
{
    public class Transaction
    {
        public Guid Id { get; protected set; }
        public string PartitionKey { get; protected set; }
        public Money Amount { get; protected set; }
        public DateTime Timestamp { get; protected set; }
        public  TransactionStatus Status { get; protected set; }
        public TransactionType TransactionType { get; protected set; }
        public bool IsContra { get; }
        public string Notes { get; protected set; }
        public TransactionAccountInfo SourceAccount { get; protected set; }
        public TransactionAccountInfo DestAccount { get; protected set; }
        public TransactionFailReason FailReason { get; protected set; }
        public DateTime? DateCompleted { get; protected set; }
        public List<TransactionLink> Links { get; protected set; }

        public CompleteTransactionData CompleteTransactionData { get; protected set; }

        public Transaction(Guid id, string partitionKey, Money amount, TransactionType transactionType, bool isContra, DateTime timestamp,
            TransactionAccountInfo sourceAccount,
            TransactionAccountInfo destAccount,
            string notes,
            List<TransactionLink> links = null)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException($"'{nameof(partitionKey)}' cannot be null, empty or whitespace.", nameof(partitionKey));
            }

            if (sourceAccount is null)
            {
                throw new ArgumentNullException(nameof(sourceAccount));
            }

            if (destAccount is null)
            {
                throw new ArgumentNullException(nameof(destAccount));
            }

            if (string.IsNullOrWhiteSpace(notes))
            {
                throw new ArgumentException($"'{nameof(notes)}' cannot be null, empty or whitespace.", nameof(notes));
            }
            Id = id;
            PartitionKey = partitionKey;
            Amount = amount;
            TransactionType = transactionType;
            IsContra = isContra;
            Timestamp = timestamp;
            SourceAccount = sourceAccount;
            DestAccount = destAccount;
            Notes = notes;
            FailReason = TransactionFailReason.None;
            Links = links ?? new List<TransactionLink>();
        }

        public void MarkFailed(TransactionFailReason failReason, DateTime dateFailed)
        {
            if (failReason == TransactionFailReason.None)
            {
                throw new ArgumentException($"Can not mark failed with {nameof(failReason)} == {nameof(TransactionFailReason.None)}");
            }
            if (Status == TransactionStatus.Initiated)
            {
                Status = TransactionStatus.Failed;
                FailReason = failReason;
                DateCompleted = dateFailed;
            }
        }

        public void Complete(CompleteTransactionData completeTransactionData)
        {
            if (completeTransactionData is null)
            {
                throw new ArgumentNullException(nameof(completeTransactionData));
            }

            if (Status == TransactionStatus.Initiated)
            {
                Status = TransactionStatus.Succeeded;
                CompleteTransactionData = completeTransactionData;
            }
        }

        public TransactionLink GetLink(TransactionLinkType linkType)
        {
            if (Links != null)
            {
                foreach (var item in Links)
                {
                    if (item.LinkType == linkType)
                    {
                        return item;
                    }
                }
            }
            return null;
        }
    }  
}
