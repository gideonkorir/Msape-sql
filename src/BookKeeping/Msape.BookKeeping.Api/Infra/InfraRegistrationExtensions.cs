using Microsoft.Extensions.DependencyInjection;
using Msape.BookKeeping.Api.Infra;
using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Api
{
    public static class InfraRegistrationExtensions
    {
        public static IServiceCollection AddTransactionSender(this IServiceCollection services, Func<AccountType, string> accountTypeToQueueName = null)
        {
            services.AddSingleton<ISendTransactionCommand, TransactionCommandSender>();
            services.AddSingleton(accountTypeToQueueName ?? ToQueueName);
            return services;
        }

        public static IServiceCollection AddReceiptNumberConverter(this IServiceCollection services)
        {
            return
                services.AddSingleton<ITransactionIdToReceiptNumberConverter, TransactionIdToReceiptNumberConverter>();
        }

        public static string ToQueueName(AccountType type)
        {
            return type switch
            {
                AccountType.SystemAgentFloat => "post-system-agent-float",
                AccountType.SendMoneyCharge => "post-system-customer-send-money-charge",
                AccountType.AgentFloat => "post-agent-float",
                AccountType.CustomerAccount => "post-customer-account",
                AccountType.CustomerWithdrawalCharge => "post-system-customer-withdrawal-charge",
                AccountType.TillAccount => "post-till-account",
                AccountType.CashCollectionAccount => "post-cash-collection",
                AccountType.AgentFeeAccount => "post-agent-fee",
                _ => throw new NotImplementedException($"The account type {type} has not been mapped")
            };
        }
    }
}
