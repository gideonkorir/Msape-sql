using MassTransit.Topology;
using Msape.BookKeeping.Data;
using Msape.BookKeeping.InternalContracts;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    [ConfigureConsumeTopology(false)]
    public record ReversePostTransactionToSource
    {
        public Guid PostingId { get; init; }
        public long TransactionId { get; init; }
        public TransactionType TransactionType { get; init; }
        public DateTime Timestamp { get; init; }
        public bool IsContra { get; init; }
        public AccountId Account { get; init; }
        public MoneyInfo Amount { get; init; }
        public LinkedTransactionInfo Charge { get; init; }
    }

}
