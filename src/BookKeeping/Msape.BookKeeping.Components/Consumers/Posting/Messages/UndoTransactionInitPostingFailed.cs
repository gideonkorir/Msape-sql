﻿using Msape.BookKeeping.Data;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record UndoTransactionInitPostingFailed
    {
        public DocumentRef<Guid> Transaction { get; init; }
        public DateTime Timestamp { get; init; }
        public DebitOrCreditFailReason FailReason { get; init; }
    }

}