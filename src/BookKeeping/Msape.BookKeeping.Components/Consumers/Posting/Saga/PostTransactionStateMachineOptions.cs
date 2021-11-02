using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting.Saga
{
    public class PostTransactionStateMachineOptions
    {
        public Func<AccountType, Uri> AccountTypeSendEndpoint { get; set; }
    }
}
