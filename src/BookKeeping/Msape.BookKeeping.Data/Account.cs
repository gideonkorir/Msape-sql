using System;

namespace Msape.BookKeeping.Data
{
    public class Account
    {
        public Guid Id { get; protected set; }
        public string PartitionKey { get; protected set; }
        public string AccountNumber { get; protected set; }
        public AccountClass AccountClass { get; protected set; }
        public AccountType AccountType { get; protected set; }
        public AccountStatus AccountStatus { get; protected set; }
        public decimal MinBalance { get; protected set; }
        public decimal MaxBalance { get; protected set; }
        public Money Balance { get; protected set; }

        protected Account()
        {
            //For serialization
        }

        public Account(Guid id, string partitionKey, string accountNumber, AccountClass accountClass, AccountType accountType, Money balance, decimal minBalance = 0, decimal maxBalance = decimal.MaxValue)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException($"'{nameof(partitionKey)}' cannot be null or whitespace.", nameof(partitionKey));
            }

            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                throw new ArgumentException($"'{nameof(accountNumber)}' cannot be null or whitespace.", nameof(accountNumber));
            }

            Id = id;
            PartitionKey = partitionKey;
            AccountNumber = accountNumber;
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

        public Entry Debit(DebitOrCreditInfo debitOrCreditInfo)
        {
            var (canDebit, failReason) = CanDebit(debitOrCreditInfo.Amount);
            if(!canDebit)
            {
                throw new InvalidOperationException($"Can not debit, CanDebit returned failse with reason: {failReason}");
            }
            var entry = CreateEntry(debitOrCreditInfo, EntryType.Debit);
            return entry;
        }

        public Entry Credit(DebitOrCreditInfo debitOrCreditInfo)
        {
            var (canCredit, failReason) = CanCredit(debitOrCreditInfo.Amount);
            if (!canCredit)
            {
                throw new InvalidOperationException($"Can not credit, CanCredit returned false with reason: {failReason}");
            }
            var entry = CreateEntry(debitOrCreditInfo, EntryType.Credit);
            return entry;
        }

        Entry CreateEntry(DebitOrCreditInfo debitOrCreditInfo, EntryType entryType)
        {
            if(entryType == EntryType.Debit)
            {
                switch(AccountClass)
                {
                    case AccountClass.Asset:
                        Balance += debitOrCreditInfo.Amount;
                        break;
                    default:
                        Balance -= debitOrCreditInfo.Amount;
                        break;
                }
            }
            else
            {
                switch(AccountClass)
                {
                    case AccountClass.Asset:
                        Balance -= debitOrCreditInfo.Amount;
                        break;
                    default:
                        Balance += debitOrCreditInfo.Amount;
                        break;
                }
            }
            return new Entry(
                Id: Entry.CreateId(debitOrCreditInfo.MovementInfo.Id, entryType),
                PartitionKey: PartitionKey,
                AccountId: Id,
                Amount: debitOrCreditInfo.Amount,
                EntryType: entryType,
                BalanceAfter: Balance,
                PostedDate: DateTime.UtcNow,
                TransactionInfo: debitOrCreditInfo.MovementInfo
                );
        }

        private void CheckCurrency(Money other)
        {
            if (other.Currency != Balance.Currency)
                throw new ArgumentException($"The amount currency {other.Currency} does not match balance currency {Balance.Currency}");
        }
    }

    public record DebitOrCreditInfo
    {
        public Money Amount { get; init; }
        public EntryTransactionInfo MovementInfo { get; init; }
    }
}
