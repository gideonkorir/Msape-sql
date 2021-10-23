using MassTransit.Topology;
using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    [ConfigureConsumeTopology(false)]
    public record PostTransactionToDest
    {
        public DocumentRef<Guid> Transaction { get; init; }
        public TransactionType TransactionType { get; init; }
        public bool IsContra { get; init; }
        public MoneyInfo Amount { get; init; }
        public DateTime Timestamp { get; init; }
        public DocumentRef<Guid> DestAccount { get; init; }
    }


}
