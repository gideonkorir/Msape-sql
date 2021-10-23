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
                AccountType.SystemAgentFloat => "drcr-system-agent-float",
                AccountType.SendMoneyCharge => "drcr-system-customer-send-money-charge",
                AccountType.AgentFloat => "drcr-agent-float",
                AccountType.CustomerAccount => "drcr-customer-account",
                AccountType.CustomerWithdrawalCharge => "drcr-system-customer-withdrawal-charge",
                _ => throw new NotImplementedException($"The account type {accountType} has not been mapped")
            };
            return name;
        }
    }
}
