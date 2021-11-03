using System;

namespace Msape.BookKeeping.Data
{
    public enum ChargeType
    {
        SystemCharge,
        AgentFees
    }
    public class ChargeData
    {
        public TransactionType ChargeTransactionType { get; set; }
        public DateTime FromDate { get; set; }
        public ChargeType ChargeType { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal ChargeAmount { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }
}
