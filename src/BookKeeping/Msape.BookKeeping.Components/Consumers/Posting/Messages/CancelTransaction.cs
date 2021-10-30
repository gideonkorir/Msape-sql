using MassTransit.Topology;
using Msape.BookKeeping.Data;
using Msape.BookKeeping.InternalContracts;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    [ConfigureConsumeTopology(false)]
    public record CancelTransaction
    {
        public Guid PostingId { get; init; }
        public long TransactionId { get; init; }
        public DateTime Timestamp { get; init; }
    }

}
