using System;

namespace Msape.BookKeeping.Data
{
    public class AccountSubject
    {
        public string Id { get; set; }
        public string PartitionKey { get; set; }
        public string AccountNumber { get; set; }
        public string Name { get; set; }
        public AccountType AccountType { get; set; }
        public DocumentRef<Guid> Account { get; set; }
        public DateTime DateCreatedUtc { get; set; }
    }
}
