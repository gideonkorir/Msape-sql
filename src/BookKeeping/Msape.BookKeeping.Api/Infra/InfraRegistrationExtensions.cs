using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Msape.BookKeeping.Api.Infra;
using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Api
{
    public static class InfraRegistrationExtensions
    {
        public static IServiceCollection AddCosmos(this IServiceCollection services, string connectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==")
        {
            var client = new CosmosClient(connectionString, new CosmosClientOptions()
            {
                ApplicationName = "msape",
                ConnectionMode = ConnectionMode.Direct,
                ConsistencyLevel = ConsistencyLevel.Session,
                SerializerOptions = new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });
            services.AddSingleton(client);

            services.AddSingleton<ICosmosAccount>(new CosmosAccount(client, ("msape2", "accounts"), ("msape2", "account_numbers"), ("msape2", "transactions")));

            return services;
        }


        public static IServiceCollection AddTransactionSender(this IServiceCollection services, Func<AccountType, string> accountTypeToQueueName = null)
        {
            services.AddSingleton<ISendTransactionCommand, TransactionCommandSender>();
            services.AddSingleton(accountTypeToQueueName ?? ToQueueName);
            return services;
        }

        public static string ToQueueName(AccountType type)
        {
            return type switch
            {
                AccountType.SystemAgentFloat => "drcr-system-agent-float",
                AccountType.SendMoneyCharge => "drcr-system-customer-send-money-charge",
                AccountType.AgentFloat => "drcr-agent-float",
                AccountType.CustomerAccount => "drcr-customer-account",
                AccountType.CustomerWithdrawalCharge => "drcr-system-customer-withdrawal-charge",
                _ => throw new NotImplementedException($"The account type {type} has not been mapped")
            };
        }
    }
}
