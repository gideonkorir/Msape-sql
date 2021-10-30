using Automatonymous;
using Automatonymous.Binders;
using MassTransit.Azure.ServiceBus.Core;
using System;
using System.Globalization;

namespace Msape.BookKeeping.Components.Consumers.Posting.Saga
{
    public static class TransactionStateMachineExtensions
    {
        public static EventActivityBinder<PostTransactionSaga, TransactionPostedToSource> CopyData(this EventActivityBinder<PostTransactionSaga, TransactionPostedToSource> binder)
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
                    context.Instance.ChargeInfo = SagaInstanceChargeInfo.From(context.Data, context.Instance);
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
        public static EventActivityBinder<PostTransactionSaga, PostTransactionToDestFailed> SendUndoInitiate(this EventActivityBinder<PostTransactionSaga, PostTransactionToDestFailed> binder, PostTransactionStateMachineOptions sagaOptions)
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

        public static EventActivityBinder<PostTransactionSaga, TransactionCancelled> SendFailTransaction(this EventActivityBinder<PostTransactionSaga, TransactionCancelled> binder, PostTransactionStateMachineOptions sagaOptions)
        {
            return
                binder.Send(
                    destinationAddress: sagaOptions.TransactionProcessingSendEndpoint,
                    messageFactory: context => new FailTransaction()
                    {
                        PostingId = context.Instance.CorrelationId,
                        TransactionId = context.Instance.TransactionId,
                        Timestamp = context.Instance.Timestamp,
                        FailReason = context.Instance.PostToDestData.FailReason.GetValueOrDefault()
                    },
                    contextCallback: context => context.ResponseAddress ??= context.SourceAddress
                );
        }

        public static EventActivityBinder<PostTransactionSaga, TransactionPostedToDest> SendCreditCharge(this EventActivityBinder<PostTransactionSaga, TransactionPostedToDest> binder, PostTransactionStateMachineOptions sagaOptions)
        {
            return
                binder.Send(
                    destinationAddressProvider: context => sagaOptions.AccountTypeSendEndpoint(context.Instance.ChargeInfo.DestAccount.AccountType),
                    messageFactory: context => new PostTransactionCharge()
                    {
                        PostingId = context.Instance.CorrelationId,
                        TransactionId = context.Instance.TransactionId,
                        ChargeId = context.Instance.ChargeInfo.ChargeId,
                        Timestamp = context.Instance.Timestamp
                    },
                    contextCallback: (sagaContext, context) =>
                    {
                        context.ResponseAddress ??= context.SourceAddress;
                        context.SetSessionId(sagaContext.Instance.ChargeInfo.DestAccount.AccountId.ToString(CultureInfo.InvariantCulture));
                    }
                );
        }
    }
}
