using Msape.BookKeeping.Data;
using System;
using Msape.BookKeeping.InternalContracts;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record LinkedTransactionInfo
    {
        public DocumentRef<Guid> Transaction { get; init; }
        public MoneyInfo Amount { get; init; }
        public TransactionType TransactionType { get; init; }
        public AccountId DestAccount { get; init; }
    }

}
