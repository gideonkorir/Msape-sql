using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record TransactionPostToSourceReversed
    {
        public DocumentRef<Guid> Transaction { get; init; }
        public DateTime Timestamp { get; init; }
        public MoneyInfo BalanceAfter { get; init; }
    }

}
