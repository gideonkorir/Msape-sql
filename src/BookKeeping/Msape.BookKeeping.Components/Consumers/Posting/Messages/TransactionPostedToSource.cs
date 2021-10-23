using Msape.BookKeeping.Data;
using Msape.BookKeeping.InternalContracts;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record TransactionPostedToSource
    {
        public Guid PostingId { get; init; }
        public long TransactionId { get; init; }
        public TransactionType TransactionType { get; init; }
        public bool IsContra { get; init; }
        public MoneyInfo Amount { get; init; }
        public DateTime Timestamp { get; init; }
        public AccountId SourceAccount { get; init; }
        public AccountId DestAccount { get; init; }
        public MoneyInfo SourceBalanceAfter { get; init; }
        public LinkedTransactionInfo ChargeInfo { get; init; }
    }
}
