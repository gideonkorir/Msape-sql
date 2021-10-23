using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.InternalContracts
{
    public record AccountId
    {
        public long Id { get; init; }
        public long SubjectId { get; init; }
        public string Name { get; init; }
        public string AccountNumber { get; init; }
        public AccountType AccountType { get; init; }
    }
}
