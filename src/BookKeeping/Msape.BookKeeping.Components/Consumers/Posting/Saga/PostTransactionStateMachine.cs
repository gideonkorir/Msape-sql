using Automatonymous;

namespace Msape.BookKeeping.Components.Consumers.Posting.Saga
{

    public class PostTransactionStateMachine
        : MassTransitStateMachine<PostTransactionSaga>
    {
        //events
        public Event<TransactionPostedToSource> PostedToSource { get; set; }
        public Event<TransactionPostedToDest> PostedToDest { get; set; }
        public Event<PostTransactionToDestFailed> PostToDestFailed { get; set; }
        public Event<TransactionCancelled> Cancelled { get; set; }
        public Event<TransactionChargePosted> ChargePosted { get; set; }

        //states
        public State PostingToDestAccount { get; set; }
        public State Cancelling { get; set; }
        public State PostingCharges { get; set; }

        public PostTransactionStateMachine(PostTransactionStateMachineOptions sagaOptions)
        {
            Event(() => PostedToSource, p => p.CorrelateById(id => id.Message.PostingId));
            //happy path
            Event(() => PostedToDest, p =>
            {
                p.CorrelateById(id => id.Message.PostingId);
                p.OnMissingInstance(p => p.Discard());
                p.ConfigureConsumeTopology = false; //consumer will do a Respond & not a publish
            });

            //failure path
            Event(() => PostToDestFailed, p =>
            {
                p.CorrelateById(id => id.Message.PostingId);
                p.OnMissingInstance(p => p.Discard());
                p.ConfigureConsumeTopology = false; //consumer will do a Respond & not a publish
            });
            Event(() => Cancelled, p =>
            {
                p.CorrelateById(id => id.Message.PostingId);
                p.OnMissingInstance(p => p.Discard()); //should prob handle this
                p.ConfigureConsumeTopology = false;
            });
            //charges
            Event(() => ChargePosted, p =>
            {
                p.CorrelateById(id => id.Message.PostingId);
                p.OnMissingInstance(p => p.Discard());
                p.ConfigureConsumeTopology = false;
            });

            InstanceState(c => c.InstanceState);

            Initially(
                When(PostedToSource)
                    .CopyData()
                    .TransitionTo(PostingToDestAccount)
                    .SendPostDest(sagaOptions)
                    );

            During(PostingToDestAccount,
                Ignore(PostedToSource), //already dealt with you 

                //Mark as completed and move on
                When(PostedToDest)
                    .Then(c =>
                    {
                        c.Instance.PostToDestData = new SagaEntryData()
                        {
                            BalanceAfter = c.Data.BalanceAfter,
                            Timestamp = c.Data.Timestamp
                        };
                    })
                    .IfElse(c => c.Instance.Charges is null || c.Instance.Charges.Count == 0,
                        ifContext =>
                        {
                            return ifContext.Finalize();
                        },
                        elseContext =>
                        {
                            return elseContext
                                .SendCreditCharge(sagaOptions)
                                .TransitionTo(PostingCharges);
                        }
                    ),

                //If crediting failed then we need to 1st reverse the original debit
                When(PostToDestFailed)
                    .Then(c =>
                    {
                        c.Instance.PostToDestData = new SagaEntryData()
                        {
                            BalanceAfter = c.Data.AccountBalance,
                            Timestamp = c.Data.Timestamp,
                            FailReason = c.Data.FailReason
                        };
                    })
                    .TransitionTo(Cancelling)
                    .SendCancel(sagaOptions)
                    );

            During(Cancelling,
                Ignore(PostedToSource),
                Ignore(PostToDestFailed),
                When(Cancelled)
                    .Then(c =>
                    {
                        c.Instance.CancelTxData = new SagaEntryData()
                        {
                            BalanceAfter = c.Data.BalanceAfter,
                            Timestamp = c.Data.Timestamp
                        };
                    })
                    .Finalize()
                    );

            During(PostingCharges,
                Ignore(PostedToSource),
                Ignore(PostedToDest),
                When(ChargePosted)
                    .HandleChargePosted()
                    .If(context => HasEntriesForAllCharges(context.Instance), binder =>
                    {
                        return binder.Finalize();
                    })
                );
        }

        static bool HasEntriesForAllCharges(PostTransactionSaga saga)
        {
            if (saga.Charges.Count != saga.ChargeEntries.Count)
                return false;
            bool allPosted = true;
            for (int i = 0; i < saga.Charges.Count && allPosted; i++)
            {
                var charge = saga.Charges[i];
                allPosted &= saga.ChargeEntries.Exists(c => c.ChargeId == charge.ChargeId);
            }
            return allPosted;
        }
    }
}
