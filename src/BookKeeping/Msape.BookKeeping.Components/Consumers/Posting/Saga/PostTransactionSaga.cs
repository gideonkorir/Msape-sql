﻿using Automatonymous;
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
        [JsonProperty("_ttl")]
        public string Ttl { get; set; }
        public string Type { get; private set; } = "Sagas/Transactions";
        public string InstanceState { get; set; }
        //init data
        public long TransactionId { get; set; }
        public TransactionType TransactionType { get; set; }
        public bool IsContra { get; set; }
        public MoneyInfo Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public SagaAccountInfo SourceAccount { get; set; }
        public SagaAccountInfo DestAccount { get; set; }
        //data for entries
        public SagaEntryData PostToDestData { get; set; }
        public SagaEntryData PostToSourceData { get; set; }
        public SagaInstanceChargeInfo ChargeInfo { get; set; }
        public SagaEntryData PostToChargeData { get; set; }
        public SagaEntryData UndoPostToSourceData { get; set; }
    }
}