using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers
{
    public static class AccountTypeQueueHelper
    {
        public static string GetQueueName(AccountType accountType)
        {
            var name = accountType switch
            {
                AccountType.SystemAgentFloat => "post-system-agent-float",
                AccountType.SendMoneyCharge => "post-system-customer-send-money-charge",
                AccountType.AgentFloat => "post-agent-float",
                AccountType.CustomerAccount => "post-customer-account",
                AccountType.CustomerWithdrawalCharge => "post-system-customer-withdrawal-charge",
                AccountType.TillAccount => "post-till-account",
                AccountType.CashCollectionAccount => "post-cash-collection",
                AccountType.AgentFeeAccount => "post-agent-fee",
                _ => throw new NotImplementedException($"The account type {accountType} has not been mapped")
            };
            return name;
        }
    }
}
