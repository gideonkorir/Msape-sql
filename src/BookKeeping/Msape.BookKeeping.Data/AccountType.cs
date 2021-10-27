namespace Msape.BookKeeping.Data
{
    public enum AccountType : int
    {
        //system accounts are in range 0-63
        SystemAgentFloat = 1,
        SendMoneyCharge = 2,
        CustomerWithdrawalCharge = 3,

        //other accounts begin over 64
        AgentFloat = 64,
        CustomerAccount = 65,
        TillAccount = 66,
        CashCollectionAccount = 67

    }
}
