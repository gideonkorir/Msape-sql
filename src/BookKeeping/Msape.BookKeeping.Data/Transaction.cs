using Msape.BookKeeping.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Msape.BookKeeping.Data
{
    public class Transaction
    {
        public long Id { get; protected set; }
        public long? ParentId { get; protected set; }
        public Money Amount { get; protected set; }
        public DateTime Timestamp { get; protected set; }
        public  TransactionStatus Status { get; protected set; }
        public TransactionType TransactionType { get; protected set; }
        public bool IsContra { get; protected set; }
        public TransactionAccountInfo SourceAccount { get; protected set; }
        public TransactionAccountInfo DestAccount { get; protected set; }
        public TransactionFailReason FailReason { get; protected set; }
        public DebitOrCreditFailReason? SourceFailReason { get; protected set; }
        public DebitOrCreditFailReason? DestFailReason { get; protected set; }
        public DateTime? DateCompleted { get; protected set; }
        //https://docs.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql?view=sql-server-ver15
        public ulong RowVersion { get; protected set; }
        public string Notes { get; protected set; }
        public virtual List<Entry> Entries { get; protected set; } = new List<Entry>();
        public virtual List<Transaction> Charges { get; protected set; } = new List<Transaction>();

        protected Transaction() { }

        public Transaction(long id, Money amount, TransactionType transactionType, bool isContra, DateTime timestamp,
            TransactionAccountInfo sourceAccount,
            TransactionAccountInfo destAccount,
            string notes,
            Transaction charge)
        {

            if (sourceAccount is null)
            {
                throw new ArgumentNullException(nameof(sourceAccount));
            }

            if (destAccount is null)
            {
                throw new ArgumentNullException(nameof(destAccount));
            }

            if (string.IsNullOrWhiteSpace(notes))
            {
                throw new ArgumentException($"'{nameof(notes)}' cannot be null, empty or whitespace.", nameof(notes));
            }
            Id = id;
            Amount = amount;
            TransactionType = transactionType;
            IsContra = isContra;
            Timestamp = timestamp;
            SourceAccount = sourceAccount;
            DestAccount = destAccount;
            Notes = notes;
            FailReason = TransactionFailReason.None;
            if (charge != null)
            {
                if(charge.ParentId.HasValue && charge.ParentId.Value != Id)
                {
                    throw new ArgumentException($"Charge.ParentId ({charge.ParentId}) value is different from transaction.Id ({Id})");
                }
                charge.ParentId = Id;
                Charges.Add(charge);
            }
        }

        private Money GetTotal()
        {
            if(Charges.Count == 0)
            {
                return Amount;
            }
            else
            {
                var sum = Amount;
                foreach (var c in Charges)
                    sum += c.Amount;
                return sum;
            }
        }

        public (bool hasSufficientFunds, DebitOrCreditFailReason failReason) CanPostToSource(Account account)
        {
            var total = GetTotal();

            return IsContra switch
            {
                true => account.CanCredit(total),
                _ => account.CanDebit(total)
            };
        }

        public DebitOrCreditFailReason? PostToSource(Account account)
        {
            if (Status != TransactionStatus.Pending)
                throw new InvalidOperationException($"Transaction {Id} is not in status Pending instead is in status {Status}");

            var (hasSufficientFunds, failReason) = CanPostToSource(account);

            var now = DateTime.UtcNow;
            if (!hasSufficientFunds)
            {
                Charges.ForEach(c => c.MarkFailed(TransactionFailReason.ParentTransactionFailed, now));
                MarkFailed(TransactionFailReason.FailedToPostToSource, now);
                SourceFailReason = failReason;
                return failReason;
            }
            else
            {
                //Start by doing the charge transaction if it's not null. The reason for that
                //is that we can then easily get the final transaction balance given the parent
                //transaction.
                //For statement since we don't need upto the millisecond time we can easily replace the
                //timestamp with that of the transaction i.e. we can have both an entry and statement timestamp
                Charges.ForEach(c => PostToSrcImpl(c, account));
                PostToSrcImpl(this, account);
                return null;
            }

            static void PostToSrcImpl(Transaction tx, Account account)
            {
                var entry = tx.IsContra switch
                {
                    true => account.Credit(tx.Id, tx.Amount),
                    _ => account.Debit(tx.Id, tx.Amount)
                };
                tx.Entries.Add(entry);
                tx.Status = TransactionStatus.Initiated;
            }
        }

        public (bool canPost, DebitOrCreditFailReason failReason) CanPostToDest(Account account)
        {
            return IsContra switch
            {
                true => account.CanDebit(Amount),
                _ => account.CanCredit(Amount)
            };
        }

        public DebitOrCreditFailReason? PostToDestination(Account account)
        {
            if (Status != TransactionStatus.Initiated)
                throw new InvalidOperationException($"The transaction must be in initiated state in order to post to destination");
            
            var (canReceive, failReason) = CanPostToDest(account);
            if(!canReceive)
            {
                DestFailReason = failReason;
                return failReason;
            }
            else
            {
                var entry = IsContra switch
                {
                    true => account.Debit(Id, Amount),
                    _ => account.Credit(Id, Amount)
                };
                Entries.Add(entry);
                Status = TransactionStatus.Succeeded;
                return null;
            }
        }

        public DebitOrCreditFailReason? ReversePostToSource(Account account)
        {

            if (Status != TransactionStatus.Initiated)
                throw new InvalidOperationException($"The transaction must be in initiated state in order to reverse post to source");
            var result = PostToDestination(account);
            if(Status == TransactionStatus.Succeeded)
            {
                Status = TransactionStatus.Cancelled;
            }
            return result;
        }

        public void MarkFailed(TransactionFailReason failReason, DateTime dateFailed)
        {
            if (failReason == TransactionFailReason.None)
            {
                throw new ArgumentException($"Can not mark failed with {nameof(failReason)} == {nameof(TransactionFailReason.None)}");
            }
            if (Status == TransactionStatus.Pending)
            {
                Status = TransactionStatus.Failed;
                FailReason = failReason;
                DateCompleted = dateFailed;
            }
        }

        private EntryType SourceEntryType => IsContra ? EntryType.Credit : EntryType.Debit;
        private EntryType DestEntryType => IsContra ? EntryType.Debit : EntryType.Credit;

        public Entry GetSourceEntry()
            => Entries.Find(c => c.EntryType == SourceEntryType);

        public Entry GetDestEntry()
            => Entries.Find(c => c.EntryType == DestEntryType);
    }  
}
