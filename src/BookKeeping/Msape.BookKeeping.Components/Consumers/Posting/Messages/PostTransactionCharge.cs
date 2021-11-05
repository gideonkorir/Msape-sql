using MassTransit.Topology;
using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    [ConfigureConsumeTopology(false)]
    public record PostTransactionCharge
    {
        public Guid PostingId { get; init; }
        public ulong ChargeId { get; init; }
        public ulong TransactionId { get; init; }
        public DateTime Timestamp { get; init; }
    }


}
