using System;

namespace Msape.BookKeeping.Data
{
    public record Entry
    {
        public ulong TransactionId { get; init; }
        public long AccountId { get; init; }
        public EntryType EntryType { get; init; }
        public Money BalanceAfter { get; init; }
        public DateTime PostedDate { get; init; }
        public bool IsPlus { get; init; }
    }
}
