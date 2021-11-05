namespace Msape.BookKeeping.Api.Infra
{
    public interface ITransactionIdToReceiptNumberConverter
    {
        string Convert(ulong transactionId);
    }
}