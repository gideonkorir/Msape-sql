using Automatonymous;
using Automatonymous.Binders;
using GreenPipes;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Consumers.Posting.Saga
{
    public static class TransactionStateMachineExtensions
    {
        public static EventActivityBinder<PostTransactionSaga, TransactionPostedToSource> CopyData(this EventActivityBinder<PostTransactionSaga, TransactionPostedToSource> binder, PostTransactionStateMachineOptions sagaOptions)
        {
            return binder
                .Then(context =>
                {
                    context.Instance.Amount = context.Data.Amount;
                    context.Instance.CreateDateUtc = DateTime.UtcNow;
                    context.Instance.DestAccount = SagaAccountInfo.FromAccountId(context.Data.DestAccount);
                    context.Instance.SourceAccount = SagaAccountInfo.FromAccountId(context.Data.SourceAccount);
                    context.Instance.TransactionId = context.Data.TransactionId;
                    context.Instance.TransactionType = context.Data.TransactionType;
                    context.Instance.IsContra = context.Data.IsContra;
                    context.Instance.Timestamp = context.Data.Timestamp;
                    context.Instance.PostToSourceData = new SagaEntryData()
                    {
                        BalanceAfter = context.Data.SourceBalanceAfter,
                        Timestamp = context.Data.Timestamp
                    };
                    context.Instance.Charges.Add(SagaInstanceChargeInfo.From(context.Data, context.Instance));
                    context.Instance.Ttl = sagaOptions.TtlProvider(context.Data.TransactionType);
                });
        }
        public static EventActivityBinder<PostTransactionSaga, TransactionPostedToSource> SendPostDest(this EventActivityBinder<PostTransactionSaga, TransactionPostedToSource> binder, PostTransactionStateMachineOptions sagaOptions)
        {
            return
                binder.Send(
                    destinationAddressProvider: context => sagaOptions.AccountTypeSendEndpoint(context.Instance.DestAccount.AccountType),
                    messageFactory: context => new PostTransactionToDest()
                    {
                        PostingId = context.Instance.CorrelationId,
                        TransactionId = context.Instance.TransactionId,
                        Timestamp = context.Instance.Timestamp
                    },
                    contextCallback: (sagaContext, sendContext) =>
                    {
                        sendContext.ResponseAddress ??= sendContext.SourceAddress;
                        sendContext.SetSessionId(sagaContext.Instance.DestAccount.AccountId.ToString(CultureInfo.InvariantCulture));
                    }
                );
        }
        public static EventActivityBinder<PostTransactionSaga, PostTransactionToDestFailed> SendCancel(this EventActivityBinder<PostTransactionSaga, PostTransactionToDestFailed> binder, PostTransactionStateMachineOptions sagaOptions)
        {
            return
                binder.Send(
                    destinationAddressProvider: context => sagaOptions.AccountTypeSendEndpoint(context.Instance.SourceAccount.AccountType),
                    messageFactory: context => new CancelTransaction()
                    {
                        PostingId = context.Instance.CorrelationId,
                        TransactionId = context.Data.TransactionId,
                        Timestamp = context.Instance.Timestamp
                    },
                    contextCallback: context => context.ResponseAddress ??= context.SourceAddress
                );
        }

        public static EventActivityBinder<PostTransactionSaga, TransactionPostedToDest> SendCreditCharge(this EventActivityBinder<PostTransactionSaga, TransactionPostedToDest> binder, PostTransactionStateMachineOptions sagaOptions)
        {
            return binder.ThenAsync(async context =>
            {
                var tasks = new List<Task>(context.Instance.Charges.Count);
                foreach (var charge in context.Instance.Charges)
                {
                    var endpointUri = sagaOptions.AccountTypeSendEndpoint(charge.DestAccount.AccountType);
                    var sendEndpoint = await context.GetSendEndpoint(endpointUri).ConfigureAwait(false);
                    var sessionId = charge.DestAccount.AccountId.ToString(CultureInfo.InvariantCulture);
                    var task = sendEndpoint.Send(new PostTransactionCharge()
                    {
                        PostingId = context.Instance.CorrelationId,
                        TransactionId = context.Instance.TransactionId,
                        ChargeId = charge.ChargeId,
                        Timestamp = context.Instance.Timestamp
                    },
                    sendContext =>
                    {
                        sendContext.ResponseAddress ??= sendContext.SourceAddress;
                        sendContext.SetSessionId(sessionId);
                    }
                    );
                }
            });
        }

        public static EventActivityBinder<PostTransactionSaga, TransactionChargePosted> HandleChargePosted(this EventActivityBinder<PostTransactionSaga, TransactionChargePosted> binder)
        {
            return
            binder.Then(context =>
            {
                var postData = context.Instance.ChargeEntries.Find(c => c.ChargeId == context.Data.ChargeId);
                if (postData == null)
                {
                    context.Instance.ChargeEntries.Add(new ChargeSagaEntryData()
                    {
                        ChargeId = context.Data.ChargeId,
                        BalanceAfter = context.Data.BalanceAfter,
                        Timestamp = context.Data.Timestamp
                    });
                }
            });
        }
    }
}
