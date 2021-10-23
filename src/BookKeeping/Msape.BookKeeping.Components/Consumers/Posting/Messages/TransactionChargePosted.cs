using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record TransactionChargePosted
    {
        public DocumentRef<Guid> Transaction { get; init; }
        public DocumentRef<Guid> Parent { get; init; }
        public DocumentRef<Guid> Account { get; init; }
        public MoneyInfo BalanceAfter { get; init; }
        public DateTime Timestamp { get; init; }
    }


}
