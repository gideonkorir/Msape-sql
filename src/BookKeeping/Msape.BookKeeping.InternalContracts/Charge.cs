using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.InternalContracts
{
    public record Charge
    {
        public long Id { get; init; }
        public decimal Amount { get; init; }
        public int Currency { get; init; }
        public TransactionType TransactionType { get; init; }
        public AccountId PayToAccount { get; init; }
    }
}
