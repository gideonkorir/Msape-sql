using Automatonymous;
using Automatonymous.Binders;
using System;

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
                    context.Instance.DestAccount = context.Data.DestAccount.ToTransactionAccountInfo();
                    context.Instance.SourceAccount = context.Data.SourceAccount.ToTransactionAccountInfo();
                    context.Instance.Transaction = context.Data.Transaction;
                    context.Instance.TransactionType = context.Data.TransactionType;
                    context.Instance.IsContra = context.Data.IsContra;
                    context.Instance.Timestamp = context.Data.Timestamp;
                    context.Instance.PostToSourceData = new SagaEntryData()
                    {
                        BalanceAfter = context.Data.SourceBalanceAfter,
                        Timestamp = context.Data.Timestamp
                    };
                    context.Instance.ChargeInfo = SagaInstanceChargeInfo.From(context.Data);
                });
        }
        public static EventActivityBinder<PostTransactionSaga, TransactionPostedToSource> SendPostDest(this EventActivityBinder<PostTransactionSaga, TransactionPostedToSource> binder, PostTransactionStateMachineOptions sagaOptions)
        {
            return
                binder.Send(
                    destinationAddressProvider: context => sagaOptions.AccountTypeSendEndpoint(context.Instance.DestAccount.AccountType),
                    messageFactory: context => new PostTransactionToDest()
                    {
                        Transaction = context.Instance.Transaction,
                        Timestamp = context.Instance.Timestamp,
                        DestAccount = context.Instance.DestAccount,
                        Amount = context.Instance.Amount,
                        TransactionType = context.Instance.TransactionType,
                        IsContra = context.Instance.IsContra
                    },
                    contextCallback: context => context.ResponseAddress ??= context.SourceAddress
                );
        }
        public static EventActivityBinder<PostTransactionSaga, PostTransactionToDestFailed> SendUndoInitiate(this EventActivityBinder<PostTransactionSaga, PostTransactionToDestFailed> binder, PostTransactionStateMachineOptions sagaOptions)
        {
            return
                binder.Send(
                    destinationAddressProvider: context => sagaOptions.AccountTypeSendEndpoint(context.Instance.SourceAccount.AccountType),
                    messageFactory: context => new ReversePostTransactionToSource()
                    {
                        Transaction = context.Data.Transaction,
                        Timestamp = context.Instance.Timestamp,
                        IsContra = context.Instance.IsContra,
                        Account = context.Instance.SourceAccount.ToAccountId(),
                        Amount = context.Instance.Amount,
                        TransactionType = context.Instance.TransactionType,
                        Charge = context.Instance.ChargeInfo == null ? null : new LinkedTransactionInfo()
                        {
                            Amount = context.Instance.ChargeInfo.Amount,
                            DestAccount =context.Instance.ChargeInfo.DestAccount.ToAccountId(),
                            Transaction = context.Instance.ChargeInfo.Charge,
                            TransactionType = context.Instance.ChargeInfo.TransactionType
                        }
                    },
                    contextCallback: context => context.ResponseAddress ??= context.SourceAddress
                );
        }
        public static EventActivityBinder<PostTransactionSaga, TransactionPostedToDest> SendCompleteTransaction(this EventActivityBinder<PostTransactionSaga, TransactionPostedToDest> binder, PostTransactionStateMachineOptions sagaOptions)
        {
            return
                binder.Send(
                    destinationAddress: sagaOptions.TransactionProcessingSendEndpoint,
                    messageFactory: context => new CompleteTransaction()
                    {
                        Transaction = context.Instance.Transaction,
                        Timestamp = context.Instance.PostToDestData.Timestamp,
                        DebitBalance = context.Instance.PostToSourceData.BalanceAfter,
                        DebitTimestamp = context.Instance.PostToSourceData.Timestamp,
                        CreditBalance = context.Instance.PostToDestData.BalanceAfter,
                        CreditTimestamp = context.Instance.PostToDestData.Timestamp
                    },
                    contextCallback: context => context.ResponseAddress ??= context.SourceAddress
                );
        }

        public static EventActivityBinder<PostTransactionSaga, TransactionPostToSourceReversed> SendFailTransaction(this EventActivityBinder<PostTransactionSaga, TransactionPostToSourceReversed> binder, PostTransactionStateMachineOptions sagaOptions)
        {
            return
                binder.Send(
                    destinationAddress: sagaOptions.TransactionProcessingSendEndpoint,
                    messageFactory: context => new FailTransaction()
                    {
                        Transaction = context.Instance.Transaction,
                        Timestamp = context.Instance.Timestamp,
                        FailReason = context.Instance.PostToDestData.FailReason.GetValueOrDefault()
                    },
                    contextCallback: context => context.ResponseAddress ??= context.SourceAddress
                );
        }

        public static EventActivityBinder<PostTransactionSaga, TransactionCompleted> SendCreditCharge(this EventActivityBinder<PostTransactionSaga, TransactionCompleted> binder, PostTransactionStateMachineOptions sagaOptions)
        {
            return
                binder.Send(
                    destinationAddressProvider: context => sagaOptions.AccountTypeSendEndpoint(context.Instance.ChargeInfo.DestAccount.AccountType),
                    messageFactory: context => new PostTransactionCharge()
                    {
                        Parent = context.Instance.Transaction,
                        ParentTransactionType = context.Instance.TransactionType,
                        Transaction = context.Instance.ChargeInfo.Charge,
                        Timestamp = context.Instance.Timestamp,
                        CreditAccount = context.Instance.ChargeInfo.DestAccount,
                        Amount =context.Instance.ChargeInfo.Amount,
                        TransactionType = context.Instance.ChargeInfo.TransactionType,
                        IsContra = context.Instance.IsContra
                    },
                    contextCallback: context => context.ResponseAddress ??= context.SourceAddress
                );
        }

        public static EventActivityBinder<PostTransactionSaga, TransactionChargePosted> SendCompleteCharge(this EventActivityBinder<PostTransactionSaga, TransactionChargePosted> binder, PostTransactionStateMachineOptions sagaOptions)
        {
            return
                binder.Send(
                    destinationAddressProvider: context => sagaOptions.TransactionProcessingSendEndpoint,
                    messageFactory: context => new CompleteTransactionCharge()
                    {
                        Parent = context.Instance.Transaction,
                        Transaction = context.Instance.ChargeInfo.Charge,
                        Timestamp = context.Instance.Timestamp,
                        CreditBalance = context.Data.BalanceAfter,
                        CreditTimestamp = context.Data.Timestamp,
                        DebitBalance = context.Instance.PostToSourceData.BalanceAfter,
                        DebitTimestamp = context.Instance.Timestamp
                    },
                    contextCallback: context => context.ResponseAddress ??= context.SourceAddress
                );
        }
    }
}
