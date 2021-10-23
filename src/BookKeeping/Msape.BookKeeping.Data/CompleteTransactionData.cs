using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Data
{
    public record CompleteTransactionData(DateTime CompleteTimestamp, Money SourceBalance, DateTime SourcePostedDate, Money DestBalance, DateTime DestPostedDate);
}
