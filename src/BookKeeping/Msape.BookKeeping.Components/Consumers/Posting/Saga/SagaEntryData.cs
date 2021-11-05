using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting.Saga
{
    public class SagaEntryData
    {
        public MoneyInfo BalanceAfter { get; set; }
        public DateTime Timestamp { get; set; }
        public DebitOrCreditFailReason? FailReason { get; set; }
    }

    public class ChargeSagaEntryData
    {
        public MoneyInfo BalanceAfter { get; set; }
        public DateTime Timestamp { get; set; }
        public ulong ChargeId { get; set; }
    }
}
