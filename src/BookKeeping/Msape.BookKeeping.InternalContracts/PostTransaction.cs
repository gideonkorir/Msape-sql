using Msape.BookKeeping.Data;
using System;
using System.Collections.Generic;

namespace Msape.BookKeeping.InternalContracts
{
    public record PostTransaction
    {
        public Guid PostingId { get; init; }
        public string ReceiptNumber { get; init; }
        public ulong TransactionId { get; init; }
        public TransactionType TransactionType { get; init; }
        public bool IsContra { get; init; }
        public AccountId SourceAccount { get; init; }
        public AccountId DestAccount { get; init; }
        public string ExternalReference { get; init; }
        public decimal Amount { get; init; }
        public Currency Currency { get; init; }
        public DateTime Timestamp { get; init; }
        public List<Charge> Charges { get; init; } = new List<Charge>();
        public TransactionMetadata.UserData Metadata { get; init; }

    }
}
