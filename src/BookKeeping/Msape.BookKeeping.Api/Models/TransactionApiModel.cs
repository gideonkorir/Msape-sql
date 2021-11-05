using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Api.Models
{
    public record TransactionApiModel(ulong Id, DateTime Timestamp, string TransactionType, Money Amount)
    {
        public static TransactionApiModel Create(Transaction transaction)
            => new(transaction.Id, transaction.Timestamp, transaction.TransactionType.ToString(), transaction.Amount);
    }
}
