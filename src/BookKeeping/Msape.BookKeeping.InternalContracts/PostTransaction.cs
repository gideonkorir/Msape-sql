using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.InternalContracts
{
    public record PostTransaction
    {
        public Guid Id { get; init; }
        public TransactionType TransactionType { get; init; }
        public bool IsContra { get; init; }
        public AccountId DebitAccountId { get; init; }
        public AccountId CreditAccountId { get; init; }
        public string ExternalReference { get; init; }
        public decimal Amount { get; init; }
        public int Currency { get; init; }
        public DateTime Timestamp { get; init; }
        public Charge Charge { get; init; }

    }
}
