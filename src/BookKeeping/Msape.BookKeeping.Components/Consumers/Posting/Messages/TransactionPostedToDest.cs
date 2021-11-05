using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record TransactionPostedToDest
    {
        public Guid PostingId { get; init; }
        public ulong TransactionId { get; init; }
        public long AccountId { get; init; }
        public MoneyInfo BalanceAfter { get; init; }
        public DateTime Timestamp { get; init; }
    }

}
