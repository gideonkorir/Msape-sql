using Msape.BookKeeping.Data;
using System;
using Msape.BookKeeping.InternalContracts;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record TransactionCompleted
    {
        public DocumentRef<Guid> Transaction { get; init; }
        public TransactionType TransactionType { get; init; }
        public bool IsContra { get; set; }
        public MoneyInfo Amount { get; init; }
        public DateTime Timestamp { get; init; }
        public AccountId SourceAccount { get; init; }
        public AccountId DestAccount { get; init; }
        public MoneyInfo SourceBalance { get; init; }
        public MoneyInfo DestBalance { get; init; }
        public LinkedTransactionInfo ChargeInfo { get; init; }
    }

}
