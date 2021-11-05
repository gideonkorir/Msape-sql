using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record PostTransactionToDestFailed
    {
        public Guid PostingId { get; init; }
        public ulong TransactionId { get; init; }
        public long AccountId { get; init; }
        public MoneyInfo AccountBalance { get; init; }
        public DebitOrCreditFailReason FailReason { get; init; }
        public DateTime Timestamp { get; init; }
    }

}
