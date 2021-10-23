using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.InternalContracts
{
    public record AccountId
    {
        public Guid Id { get; init; }
        public string PartitionKey { get; init; }
        public string Name { get; init; }
        public string AccountNumber { get; init; }
        public AccountType AccountType { get; init; }
    }
}
