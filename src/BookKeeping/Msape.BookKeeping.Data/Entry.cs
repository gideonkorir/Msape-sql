using System;
using System.Globalization;

namespace Msape.BookKeeping.Data
{
    public record Entry(string Id, string PartitionKey, Guid AccountId, Money Amount, 
        EntryType EntryType, Money BalanceAfter,  DateTime PostedDate, EntryTransactionInfo TransactionInfo
        )
    {
        public static string CreateId(Guid transactionId, EntryType entryType)
            => $"{ToString(transactionId)}:{ToString(entryType)}";
        private static string ToString(Guid id)
            => id.ToString("D", CultureInfo.InvariantCulture);
        private static string ToString(EntryType entryType)
            => entryType switch
            {
                EntryType.Credit => "CREDIT",
                _ => "DEBIT"
            };
    }

    public record EntryTransactionInfo(Guid Id, string PartitionKey, DateTime Timestamp, TransactionType TransactionType);
}
