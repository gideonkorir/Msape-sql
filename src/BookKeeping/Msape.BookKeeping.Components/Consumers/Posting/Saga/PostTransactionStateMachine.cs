using Automatonymous;

namespace Msape.BookKeeping.Components.Consumers.Posting.Saga
{

    public class PostTransactionStateMachine
        : MassTransitStateMachine<PostTransactionSaga>
    {
        //events
        public Event<TransactionPostedToSource> PostedToSource { get; set; }
        public Event<TransactionPostedToDest> PostedToDest { get; set; }
        public Event<TransactionCompleted> TransactionCompleted { get; set; }
        public Event<PostTransactionToDestFailed> PostToDestFailed { get; set; }
        public Event<TransactionPostToSourceReversed> PostToSourceReversed { get; set; }
        public Event<TransactionFailed> TransactionFailed { get; set; }
        public Event<TransactionChargePosted> ChargePosted { get; set; }
        public Event<TransactionChargeCompleted> ChargeCompleted { get; set; }

        //states
        public State PostingToDestAccount { get; set; }
        public State ReversingAtSource { get; set; }
        public State MarkingAsCompleted { get; set; }
        public State MarkingAsFailed { get; set; }
        public State PostingCharge { get; set; }
        public State MarkingChargeAsCompleted { get; set; }

        public PostTransactionStateMachine(PostTransactionStateMachineOptions sagaOptions)
        {
            Event(() => PostedToSource, p => p.CorrelateById(id => id.Message.Transaction.Id));
            //happy path
            Event(() => PostedToDest, p =>
            {
                p.CorrelateById(id => id.Message.Transaction.Id);
                p.OnMissingInstance(p => p.Discard());
                p.ConfigureConsumeTopology = false; //consumer will do a Respond & not a publish
            });
            Event(() => TransactionCompleted, p => p.CorrelateById(id => id.Message.Transaction.Id).OnMissingInstance(p => p.Discard()));
            //failure path
            Event(() => PostToDestFailed, p =>
            {
                p.CorrelateById(id => id.Message.Transaction.Id);
                p.OnMissingInstance(p => p.Discard());
                p.ConfigureConsumeTopology = false; //consumer will do a Respond & not a publish
            });
            Event(() => TransactionFailed, p => p.CorrelateById(id => id.Message.Transaction.Id).OnMissingInstance(p => p.Discard()));
            Event(() => PostToSourceReversed, p =>
            {
                p.CorrelateById(id => id.Message.Transaction.Id);
                p.OnMissingInstance(p => p.Discard()); //should prob handle this
                p.ConfigureConsumeTopology = false;
            });
            //charges
            Event(() => ChargePosted, p =>
            {
                p.CorrelateById(id => id.Message.Parent.Id);
                p.OnMissingInstance(p => p.Discard());
                p.ConfigureConsumeTopology = false;
            });
            Event(() => ChargeCompleted, p => p.CorrelateById(id => id.Message.Parent.Transaction.Id).OnMissingInstance(p => p.Discard()));

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
                    .TransitionTo(MarkingAsCompleted)
                    .SendCompleteTransaction(sagaOptions),

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
                    .TransitionTo(ReversingAtSource)
                    .SendUndoInitiate(sagaOptions)
                    );

            During(ReversingAtSource,
                Ignore(PostedToSource),
                Ignore(PostToDestFailed),
                Ignore(TransactionFailed),
                When(PostToSourceReversed)
                    .Then(c =>
                    {
                        c.Instance.UndoPostToSourceData = new SagaEntryData()
                        {
                            BalanceAfter = c.Data.BalanceAfter,
                            Timestamp = c.Data.Timestamp
                        };
                    })
                    .TransitionTo(MarkingAsFailed)
                    .SendFailTransaction(sagaOptions)
                    );

            During(MarkingAsFailed,
                Ignore(PostedToSource),
                Ignore(PostToDestFailed),
                Ignore(PostToSourceReversed),
                When(TransactionFailed)
                    .Finalize()
                );

            During(MarkingAsCompleted,
                Ignore(PostedToSource),
                Ignore(PostedToDest),
                When(TransactionCompleted)
                    .IfElse(c => c.Instance.ChargeInfo is null,
                        ifContext =>
                        {
                            return ifContext.Finalize();
                        },
                        elseContext =>
                        {
                            return elseContext
                                .SendCreditCharge(sagaOptions)
                                .TransitionTo(PostingCharge);
                        }
                        )
                    );

            During(PostingCharge,
                Ignore(PostedToSource),
                Ignore(PostedToDest),
                Ignore(TransactionCompleted),
                Ignore(ChargeCompleted),
                When(ChargePosted)
                    .Then(context =>
                    {
                        context.Instance.PostToChargeData = new SagaEntryData()
                        {
                            BalanceAfter = context.Data.BalanceAfter,
                            Timestamp = context.Data.Timestamp
                        };
                    })
                    .SendCompleteCharge(sagaOptions)
                    .TransitionTo(MarkingChargeAsCompleted)
                    );

            During(MarkingChargeAsCompleted,
                Ignore(PostedToSource),
                Ignore(PostedToDest),
                Ignore(TransactionCompleted),
                Ignore(ChargePosted),
                When(ChargeCompleted)
                    .Finalize()
                );
        }
    }
}
