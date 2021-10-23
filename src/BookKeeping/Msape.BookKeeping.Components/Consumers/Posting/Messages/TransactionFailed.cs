using Msape.BookKeeping.Data;
using Msape.BookKeeping.InternalContracts;
using System;
using System.Collections.Generic;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record TransactionFailed
    {
        public Guid PostingId { get; init; }
        public long TransactionId { get; init; }
        public DateTime Timestamp { get; init; }
        public TransactionType TransactionType { get; init; }
        public bool IsContra { get; init; }
        public TransactionAccountInfo SourceAccount { get; init; }
        public TransactionAccountInfo DestAccount { get; init; }
        public TransactionFailReason FailReason { get; init; }
        public MoneyInfo Amount { get; init; }
        public DateTime CompletedDate { get; init; }
        public List<LinkedTransactionInfo> ChargeInfo { get; init; } = new List<LinkedTransactionInfo>();
    }

}
