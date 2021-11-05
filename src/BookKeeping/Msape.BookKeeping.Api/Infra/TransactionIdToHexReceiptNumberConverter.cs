using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Infra
{
    public class TransactionIdToHexReceiptNumberConverter : ITransactionIdToReceiptNumberConverter
    {
        private readonly int _minLength;
        private readonly Random _random = new ();

        public TransactionIdToHexReceiptNumberConverter(int minLength = 10)
        {
            _minLength = minLength;
        }

        public string Convert(ulong transactionId)
        {
            var receipt = string.Format("{0:X}", transactionId);
            if(receipt.Length < _minLength)
            {
                receipt = string.Create(_minLength, (receipt, _random), (span, state) =>
                {
                    //se values > F since we are doing hex
                    var (rec, rand) = state;
                    int start = 0;
                    for(; start < (span.Length - rec.Length); start ++)
                    {
                        span[start] = (char)('G' + rand.Next(0, 20));
                    }
                    for(int i= 0; start < span.Length; start++, i++)
                    {
                        span[start] = rec[i];
                    }
                });
            }
            return receipt;
        }
    }
}
