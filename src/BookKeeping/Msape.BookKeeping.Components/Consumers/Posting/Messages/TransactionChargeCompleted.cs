using Msape.BookKeeping.Data;
using System;
using Msape.BookKeeping.InternalContracts;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record TransactionChargeCompleted
    {
        public Guid PostingId { get; init; }
        public long TransactionId { get; init; }
        public long ChargeId { get; init; }
        public TransactionType TransactionType { get; init; }
        public bool IsContra { get; init; }
        public MoneyInfo Amount { get; init; }
        public DateTime Timestamp { get; init; }
        public AccountId SourceAccount { get; init; }
        public AccountId DestAccount { get; init; }
        public MoneyInfo SourceBalance { get; init; }
        public MoneyInfo DestBalance { get; init; }
        public LinkedTransactionInfo Parent { get; init; }
    }

}
