using System;

namespace Msape.BookKeeping.Data
{
    public enum ChargeType
    {
        SystemCharge = 1,
        AgentFees = 2
    }
    public class ChargeData
    {
        public long Id { get; set; }
        public TransactionType ChargeTransactionType { get; set; }
        public DateTime FromDate { get; set; }
        public ChargeType ChargeType { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal ChargeAmount { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }
}
