namespace Msape.BookKeeping.Data
{
    public enum TransactionType
    {
        /// <summary>
        /// Should never be used in code
        /// </summary>
        Unknown = 0,
        TransactionCharge = 1,
        AgentFees = 2,
        AgentFloatTopup = 16,
        CustomerTopup = 17,
        CustomerSendMoney = 18,
        CustomerWithdrawal = 19,
        PaymentToTill = 20,
        BillPayment = 21
    }
}
