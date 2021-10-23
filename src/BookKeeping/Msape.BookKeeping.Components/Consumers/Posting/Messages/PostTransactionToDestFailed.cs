using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record PostTransactionToDestFailed
    {
        public DocumentRef<Guid> Transaction { get; init; }
        public DocumentRef<Guid> Account { get; init; }
        public MoneyInfo AccountBalance { get; init; }
        public DebitOrCreditFailReason FailReason { get; init; }
        public DateTime Timestamp { get; init; }
    }

}
