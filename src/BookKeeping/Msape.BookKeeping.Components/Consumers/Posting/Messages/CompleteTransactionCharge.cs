using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record CompleteTransactionCharge
    {
        public DocumentRef<Guid> Parent { get; init; }
        public DocumentRef<Guid> Transaction { get; init; }
        public DateTime Timestamp { get; init; }
        public MoneyInfo DebitBalance { get; init; }
        public DateTime DebitTimestamp { get; init; }
        public MoneyInfo CreditBalance { get; init; }
        public DateTime CreditTimestamp { get; init; }
    }

}
