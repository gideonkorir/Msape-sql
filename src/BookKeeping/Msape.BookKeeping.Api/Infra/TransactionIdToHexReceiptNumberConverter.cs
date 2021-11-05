using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Infra
{
    public class TransactionIdToHexReceiptNumberConverter : ITransactionIdToReceiptNumberConverter
    {
        public TransactionIdToHexReceiptNumberConverter()
        {
        }

        public string Convert(ulong transactionId)
        {
            return string.Format("{0:X}", transactionId);
        }
    }
}
