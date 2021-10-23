namespace Msape.BookKeeping.Data
{
    public enum TransactionStatus
    {
        Pending,
        Initiated,
        Succeeded,
        Cancelled,
        Failed
    }

    public enum TransactionFailReason
    {
        None,
        FailedToPostToSource,
        FailedToPostToDest,
        ParentTransactionFailed
    }
}
