using MassTransit.Topology;
using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    [ConfigureConsumeTopology(false)]
    public record PostTransactionCharge
    {
        public DocumentRef<Guid> Transaction { get; init; }
        public DocumentRef<Guid> Parent { get; init; }
        public TransactionType TransactionType { get; init; }
        public MoneyInfo Amount { get; init; }
        public DateTime Timestamp { get; init; }
        public DocumentRef<Guid> CreditAccount { get; init; }
        public TransactionType ParentTransactionType { get; init; }
        public bool IsContra { get; init; }
    }


}
