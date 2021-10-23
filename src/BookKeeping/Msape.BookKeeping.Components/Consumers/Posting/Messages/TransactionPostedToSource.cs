using MassTransit.Topology;
using Msape.BookKeeping.Data;
using System;
using Msape.BookKeeping.InternalContracts;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record TransactionPostedToSource
    {
        public DocumentRef<Guid> Transaction { get; init; }
        public TransactionType TransactionType { get; init; }
        public bool IsContra { get; init; }
        public MoneyInfo Amount { get; init; }
        public DateTime Timestamp { get; init; }
        public AccountId SourceAccount { get; init; }
        public AccountId DestAccount { get; init; }
        public MoneyInfo SourceBalanceAfter { get; init; }
        public LinkedTransactionInfo ChargeInfo { get; init; }
    }
}
