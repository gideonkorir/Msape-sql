# How does posting work

The system aims to post transaction to accounts in near realtime rather than in batch i.e. the entries are created near immediately rather than in than
as part of a batch process. The process of processing is:

1. Receive request from user and send `PostTransaction` command to queue
2. Consumer receives the command and creates the account + immediately posts (including charges) to the source account.
   a. A `TransactionPostedToSource` internal event is raised if transaction is successfully posted to source
   b. A `TransactionFailed` event is raised if transaction can't be posted to source. **Posting stops here**.
3. A `TransactionPostingSaga` is created off the `TransactionPostedToSource` event. The saga contains all information to continue posting
4. The saga sends a `PostTransansactionToDest` command to the `PostTransactionToDestConsumer` consumer. Consumer will handle the command and:
   a. Complete successfully and respond back to saga with `TransactionPostedToDest`
   b. Fail to post and respond back to saga with `TransactionPostToDestFailed`
5. On receive `TransactionPostedToDest` from (4) then:
   a. Post all transaction charges, if any. This involves sending `PostTransactionCharge` commands for each charge.
   b. Saga will wait for `TransactionChargePosted` for all transaction charges. **Posting to charge accounts isn't expected to fail**. The assumption here is that charges should never fail, if they do it is most likely an issue with the system and should be handled that way.
6. On `TransactionPostToDestFailed` proceed to cancel the transaction, it does so by sending a `CancelTransaction` command that will cause the original posting
   source to be undone (credit if prev was debit or debit if prev was credit) and the transaction status to change to canceled. 
6. Saga will transition to final state. Saga will publish a (not yet implemented) `TransactionPostingCompleted` or `TransactionPostingFailed` event

## Handling concurrency while posting

For accounts with low transaction volumes, it would be simple to just update all accounts in the `PostTransaction` consumer handler with retry on concurrency exceptions; for transactions that involve high volume transactions the concurrency exception could mean lots of retries
and performance issues. For simplicity, I went with option of posting to each account separately - this works for both low and high tx volume accounts whilst at the same time only requiring a single code path.

Concurrency is controlled by:

1. Using [session queues](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-sessions) for commands (`PostTransaction`, `PostTransactionToDest`, `PostTransactionCharge`) with session key set to target account Id. All commands to the same account will be part of the same session
2. Account has [row version](https://docs.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql?view=sql-server-ver15)
3. Posting is [idempotent](https://www.restapitutorial.com/lessons/idempotency.html). The consumer follows a *check then execute* pattern; applying the same operation again might cause it to fail so there is the need to check to ensure it hasn't been operated yet

## Simplifying the saga

The account consumers update the transaction and the account when handling commands, this simplifies the saga so that the saga doesn't have to deal with intermediate states i.e. there is no need for separate update accounts and update transactions commands and states.