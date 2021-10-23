namespace Msape.BookKeeping.Data
{
    public enum TransactionStatus
    {
        Initiated,
        Succeeded,
        Failed
    }

    public enum TransactionFailReason
    {
        None,
        FailedToInitiate,
        FailedToReceive,
        ParentTransactionFailed
    }
}
