namespace Msape.BookKeeping.Data
{
    public enum TransactionType
    {
        /// <summary>
        /// Should never be used in code
        /// </summary>
        Unknown = 0,
        AgentFloatTopup = 1,
        CustomerTopup = 2,
        CustomerSendMoney = 3,
        SendMoneyCharge = 4,
        CustomerWithdrawal = 5,
        CustomerWithdrawalCharge = 6,
        PaymentToTill = 7,
        BillPayment = 8
    }
}
