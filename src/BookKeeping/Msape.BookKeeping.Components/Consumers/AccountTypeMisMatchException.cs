using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers
{
    public class AccountTypeMisMatchException : Exception
    {
        public AccountTypeMisMatchException(string message, string accountNumber, AccountType expected, AccountType actual)
            : base(message)
        {
            AccountNumber = accountNumber;
            Expected = expected;
            Actual = actual;
        }

        public string AccountNumber { get; }
        public AccountType Expected { get; }
        public AccountType Actual { get; }

    }
}
