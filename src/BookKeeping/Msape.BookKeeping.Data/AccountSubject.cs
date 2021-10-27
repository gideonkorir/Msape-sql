using System;

namespace Msape.BookKeeping.Data
{
    public class AccountSubject
    {
        public long Id { get; set; }
        public string AccountNumber { get; set; }
        public string Name { get; set; }
        public AccountType AccountType { get; set; }
        public DateTime DateCreatedUtc { get; set; }
        public virtual Account Account { get; set; }
    }
}
