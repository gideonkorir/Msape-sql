using System;

namespace Msape.BookKeeping.Data
{
    public record TransactionLink : DocumentRef<Guid>
    {
        public Money Amount { get; init; }
        public TransactionType TransactionType { get; init; }
        public DateTime Timestamp { get; init; }
        public TransactionLinkType LinkType { get; init; }
        public TransactionAccountInfo DebitAccount { get; init; }
        public TransactionAccountInfo CreditAccount { get; init; }
    }
}
