using Microsoft.Azure.Cosmos;
using System;

namespace Msape.BookKeeping.Components.Infra
{
    public class TransactionalBatchFailedException : Exception
    {
        public TransactionalBatchResponse Response { get; }

        public TransactionalBatchFailedException(string message, TransactionalBatchResponse response)
            : base(message)
        {
            Response = response;
        }
    }
}
