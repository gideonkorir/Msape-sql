using System;

namespace Msape.BookKeeping.Data
{
    public class Account
    {
        public long Id { get; protected set; }
        public int PartyId { get; protected set; }
        public AccountClass AccountClass { get; protected set; }
        public AccountType AccountType { get; protected set; }
        public AccountStatus AccountStatus { get; protected set; }
        public decimal MinBalance { get; protected set; }
        public decimal MaxBalance { get; protected set; }
        public Money Balance { get; protected set; }
        //https://docs.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql?view=sql-server-ver15
        public ulong RowVersion { get; protected set; }
        public ulong Version { get; protected set; }

        protected Account()
        {
            //For serialization
        }

        public Account(long id, int partyId, AccountClass accountClass, AccountType accountType, Money balance, decimal minBalance = 0, decimal maxBalance = decimal.MaxValue)
        {
            Id = id;
            PartyId = partyId;
            AccountClass = accountClass;
            AccountType = accountType;
            AccountStatus = AccountStatus.Active;
            Balance = balance;
            MinBalance = minBalance;
            MaxBalance = maxBalance;
        }

        public (bool canCredit, DebitOrCreditFailReason failReason) CanDebit(Money amount)
        {
            CheckCurrency(amount);
            return AccountClass switch
            {
                AccountClass.Asset => 
                    ((Balance + amount).Value <= MaxBalance, DebitOrCreditFailReason.MaxLimitReached), //debiting assets increases their value
                _ => 
                    (Balance.Value >= amount.Value && (Balance - amount).Value >= MinBalance, DebitOrCreditFailReason.InsufficientBalance), //debiting non-assets decreases their value
            };
        }

        public (bool canCredit, DebitOrCreditFailReason failReason) CanCredit(Money amount)
        {
            CheckCurrency(amount);
            return AccountClass switch
            {
                AccountClass.Asset => 
                    (Balance.Value >= amount.Value && (Balance - amount).Value >= MinBalance, DebitOrCreditFailReason.InsufficientBalance), //crediting non-assets decreases their value
                _ => 
                    ((Balance + amount).Value <= MaxBalance, DebitOrCreditFailReason.MaxLimitReached) , //crediting assets increases their value
            };
        }

        public Entry Debit(ulong transactionId, Money amount)
        {
            var (canDebit, failReason) = CanDebit(amount);
            if(!canDebit)
            {
                throw new InvalidOperationException($"Can not debit, CanDebit returned failse with reason: {failReason}");
            }
            var entry = CreateEntry(transactionId, amount, EntryType.Debit);
            return entry;
        }

        public Entry Credit(ulong transactionId, Money amount)
        {
            var (canCredit, failReason) = CanCredit(amount);
            if (!canCredit)
            {
                throw new InvalidOperationException($"Can not credit, CanCredit returned false with reason: {failReason}");
            }
            var entry = CreateEntry(transactionId, amount, EntryType.Credit);
            return entry;
        }

        Entry CreateEntry(ulong transactionId, Money amount, EntryType entryType)
        {
            bool isPlus = true;
            if(entryType == EntryType.Debit)
            {
                switch(AccountClass)
                {
                    case AccountClass.Asset:
                        Balance += amount;
                        break;
                    default:
                        Balance -= amount;
                        isPlus = false;
                        break;
                }
            }
            else
            {
                switch(AccountClass)
                {
                    case AccountClass.Asset:
                        Balance -= amount;
                        isPlus = false;
                        break;
                    default:
                        Balance += amount;
                        break;
                }
            }
            Version += 1;
            return new Entry
            {
                TransactionId = transactionId,
                AccountId = Id,
                EntryType = entryType,
                BalanceAfter = Balance with { }, //clone to fix ef issue: The same entity is being tracked as different weak entity types
                PostedDate = DateTime.UtcNow,
                IsPlus = isPlus
            };
        }

        private void CheckCurrency(Money other)
        {
            if (other.Currency != Balance.Currency)
                throw new ArgumentException($"The amount currency {other.Currency} does not match balance currency {Balance.Currency}");
        }
    }

    public record DebitOrCreditInfo(long TransactionId, Money Amount);
}
