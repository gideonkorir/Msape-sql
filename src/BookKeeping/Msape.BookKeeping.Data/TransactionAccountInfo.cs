using System;

namespace Msape.BookKeeping.Data
{
    public record TransactionAccountInfo
    {
        public long AccountId { get; init; }
        public long AccountSubjectId { get; init; }
    }
}
