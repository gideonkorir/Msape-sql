using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record TransactionChargePosted
    {
        public Guid PostingId { get; init; }
        public ulong ChargeId { get; init; }
        public ulong TransactionId { get; init; }
        public long PostedToAccountId { get; init; }
        public MoneyInfo BalanceAfter { get; init; }
        public DateTime Timestamp { get; init; }
    }


}
