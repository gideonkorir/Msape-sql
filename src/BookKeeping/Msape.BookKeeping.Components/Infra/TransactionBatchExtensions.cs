using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Infra
{
    public static class TransactionBatchExtensions
    {
        public static void ThrowIfNotSuccessful(this TransactionalBatchResponse response)
        {
            if (response.IsSuccessStatusCode)
                return;

            StringBuilder builder = new ();
            builder.Append("Transactional Batch failed with message: ")
                .Append(response.ErrorMessage)
                .AppendLine(". Failed response codes:");
            int index = 0;
            foreach(var item in response)
            {
                if(item.IsSuccessStatusCode)
                {
                    builder.AppendLine($"Index {index}, Status Code {item.StatusCode}");
                }
                index += 1;
            }
            throw new TransactionalBatchFailedException(builder.ToString(), response);
        }
    }
}
