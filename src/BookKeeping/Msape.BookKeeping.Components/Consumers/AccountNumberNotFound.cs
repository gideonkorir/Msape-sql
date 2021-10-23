using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers
{
    public class AccountNumberNotFound : Exception
    {
        public AccountNumberNotFound(string message, string accountNumber, AccountType accountType)
            : base (message)
        {
            AccountNumber = accountNumber;
            AccountType = accountType;
        }

        public string AccountNumber { get; }
        public AccountType AccountType { get; }


    }
}
