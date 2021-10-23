using System;

namespace Msape.BookKeeping.Data
{
    public record TransactionAccountInfo : DocumentRef<Guid>
    {
        public string Name { get; init; }
        public string AccountNumber { get; init; }
        public AccountType AccountType { get; init; }
    }
}
