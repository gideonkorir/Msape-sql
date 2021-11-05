using Msape.BookKeeping.Data;

namespace Msape.BookKeeping.Components.Consumers.Posting.Saga
{
    public class SagaInstanceChargeInfo
    {
        public ulong ChargeId { get; set; }
        public MoneyInfo Amount { get; set; }
        public TransactionType TransactionType { get; set; }
        public SagaAccountInfo DestAccount { get; set; }

        public static SagaInstanceChargeInfo From(LinkedTransactionInfo charge)
            => charge == null ? null : new SagaInstanceChargeInfo()
            {
                ChargeId = charge.TransactionId,
                Amount = charge.Amount,
                TransactionType = charge.TransactionType,
                DestAccount = SagaAccountInfo.FromAccountId(charge.DestAccount)
            };
    }
}
