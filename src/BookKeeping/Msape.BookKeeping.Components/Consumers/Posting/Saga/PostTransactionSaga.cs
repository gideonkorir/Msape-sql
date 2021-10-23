using Automatonymous;
using Msape.BookKeeping.Data;
using Newtonsoft.Json;
using System;

namespace Msape.BookKeeping.Components.Consumers.Posting.Saga
{
    public class PostTransactionSaga
        : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public DateTime CreateDateUtc { get; set; }
        [JsonProperty("_etag")]
        public string Etag { get; set; }
        public string Type { get; private set; } = "Sagas/Transactions";
        public string InstanceState { get; set; }
        //init data
        public DocumentRef<Guid> Transaction { get; set; }
        public TransactionType TransactionType { get; set; }
        public bool IsContra { get; set; }
        public MoneyInfo Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public TransactionAccountInfo SourceAccount { get; set; }
        public TransactionAccountInfo DestAccount { get; set; }
        //data for entries
        public SagaEntryData PostToDestData { get; set; }
        public SagaEntryData PostToSourceData { get; set; }
        public SagaInstanceChargeInfo ChargeInfo { get; set; }
        public SagaEntryData PostToChargeData { get; set; }
        public SagaEntryData UndoPostToSourceData { get; set; }
    }

    public class SagaInstanceChargeInfo
    {
        public DocumentRef<Guid> Charge { get; set; }
        public MoneyInfo Amount { get; set; }
        public TransactionType TransactionType { get; set; }
        public TransactionAccountInfo DestAccount { get; set; }

        public static SagaInstanceChargeInfo From(TransactionPostedToSource @event)
            => @event.ChargeInfo == null ? null : new SagaInstanceChargeInfo()
            {
                Charge = @event.ChargeInfo.Transaction,
                Amount = @event.ChargeInfo.Amount,
                TransactionType = @event.ChargeInfo.TransactionType,
                DestAccount = @event.ChargeInfo.DestAccount.ToTransactionAccountInfo()
            };
    }
}
