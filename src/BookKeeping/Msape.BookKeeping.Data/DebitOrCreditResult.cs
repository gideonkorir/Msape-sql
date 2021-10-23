using System;

namespace Msape.BookKeeping.Data
{
    public struct DebitOrCreditResult
    {
        public Entry Entry { get; }
        public bool Successful => Entry != null;

        public DebitOrCreditResult(Entry entry)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }
    }

    public enum DebitOrCreditFailReason
    {
        InsufficientBalance,
        MaxLimitReached
    }
}
