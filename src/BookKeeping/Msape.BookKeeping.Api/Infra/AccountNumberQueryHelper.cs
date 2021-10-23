using Microsoft.Azure.Cosmos;
using Msape.BookKeeping.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Infra
{
    public static class AccountNumberQueryHelper
    {
        public static async Task<AccountSubject> GetSubject(Container container, string linkedAccountKey, string accountNumber, bool partitionKeyIsReversedAccountNumber, ItemRequestOptions requestOptions = default, CancellationToken cancellationToken = default)
        {
            try
            {
                var partKey = partitionKeyIsReversedAccountNumber ? StringUtil.Reverse(accountNumber) : accountNumber;
                var response = await container.ReadItemAsync<AccountSubject>(
                    id: linkedAccountKey,
                    partitionKey: new PartitionKey(partKey),
                    requestOptions: requestOptions,
                    cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
                var savedNumber = response.Resource;
                return savedNumber;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
