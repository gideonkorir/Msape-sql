using Microsoft.Azure.Cosmos;
using Msape.BookKeeping.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Infra
{
    public static class SystemAccountNumberHelper
    {
        public static async Task<AccountSubject> GetAgentFloatDebitAccount(Container container, ItemRequestOptions requestOptions = default, CancellationToken cancellationToken = default)
        {
            var response = await container.ReadItemAsync<AccountSubject>(
                    id: "AGENT_FLOAT",
                    partitionKey: new PartitionKey("__SYSTEM__"),
                    requestOptions: requestOptions,
                    cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
            return response.Resource;
        }

        public static async Task<AccountSubject> GetCustomerSendMoneyChargeCreditAccount(Container container, ItemRequestOptions requestOptions = default, CancellationToken cancellationToken = default)
        {
            var response = await container.ReadItemAsync<AccountSubject>(
                    id: "CUSTOMER_SEND_MONEY_CHARGE",
                    partitionKey: new PartitionKey("__SYSTEM__"),
                    requestOptions: requestOptions,
                    cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
            return response.Resource;
        }

        public static async Task<AccountSubject> GetCustomerWithdrawalChargeCreditAccount(Container container, ItemRequestOptions requestOptions = default, CancellationToken cancellationToken = default)
        {
            var response = await container.ReadItemAsync<AccountSubject>(
                    id: "CUSTOMER_WITHDRAWAL_CHARGE",
                    partitionKey: new PartitionKey("__SYSTEM__"),
                    requestOptions: requestOptions,
                    cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
            return response.Resource;
        }
    }
}
