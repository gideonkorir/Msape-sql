namespace Msape.BookKeeping.Data
{
    public enum TransactionType
    {
        /// <summary>
        /// Should never be used in code
        /// </summary>
        Unknown = 0,
        TransactionCharge = 1,
        AgentFloatTopup = 2,
        CustomerTopup = 3,
        CustomerSendMoney = 4,
        CustomerWithdrawal = 5,
        PaymentToTill = 6,
        BillPayment = 7
    }
}
