using Microsoft.Azure.Cosmos;
using System.Collections.Generic;

namespace Msape.BookKeeping.Components.Infra
{
    public class CosmosAccount : ICosmosAccount
    {
        public Container Transactions { get; }

        public Container Accounts { get; }

        public Container AccountNumbers { get; }

        public CosmosAccount(CosmosClient cosmosClient, Dictionary<string, (string databaseId, string containerId)> configuration)
        {
            var (databaseId, containerId) = configuration[nameof(Transactions)];
            Transactions = cosmosClient.GetContainer(databaseId, containerId);

            (databaseId, containerId) = configuration[nameof(Accounts)];
            Accounts = cosmosClient.GetContainer(databaseId, containerId);

            (databaseId, containerId) = configuration[nameof(AccountNumbers)];
            AccountNumbers = cosmosClient.GetContainer(databaseId, containerId);            
        }

        public CosmosAccount(CosmosClient cosmosClient, (string databaseId, string containerId) accounts,
            (string databaseId, string containerId) accountNumbers,
            (string databaseId, string containerId) transactions)
        {
            Accounts = cosmosClient.GetContainer(accounts.databaseId, accounts.containerId);
            Transactions = cosmosClient.GetContainer(transactions.databaseId, transactions.containerId);
            AccountNumbers = cosmosClient.GetContainer(accountNumbers.databaseId, accountNumbers.containerId);
        }
    }
}
