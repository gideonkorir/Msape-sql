using Msape.BookKeeping.Data;

namespace Msape.BookKeeping.Components.Consumers.Posting.Saga
{
    public class SagaInstanceChargeInfo
    {
        public long ChargeId { get; set; }
        public MoneyInfo Amount { get; set; }
        public TransactionType TransactionType { get; set; }
        public SagaAccountInfo DestAccount { get; set; }

        public static SagaInstanceChargeInfo From(TransactionPostedToSource @event, PostTransactionSaga saga)
            => @event.ChargeInfo == null ? null : new SagaInstanceChargeInfo()
            {
                ChargeId = @event.ChargeInfo.TransactionId,
                Amount = @event.ChargeInfo.Amount,
                TransactionType = @event.ChargeInfo.TransactionType,
                DestAccount = SagaAccountInfo.FromAccountId(@event.ChargeInfo.DestAccount)
            };
    }
}
