using Msape.BookKeeping.Components.Consumers.Posting;
using Msape.BookKeeping.Data;
using Msape.BookKeeping.InternalContracts;

namespace Msape.BookKeeping.Components.Consumers
{
    internal static class HelperExtensions
    {
        public static TransactionAccountInfo ToTransactionAccountInfo(this AccountId accountId)
            => new()
            {
                Id = accountId.Id,
                Name = accountId.Name,
                PartitionKey = accountId.PartitionKey,
                AccountType = accountId.AccountType,
                AccountNumber = accountId.AccountNumber
            };

        public static AccountId ToAccountId(this TransactionAccountInfo accountInfo)
            => new()
            {
                Id = accountInfo.Id,
                Name = accountInfo.Name,
                PartitionKey = accountInfo.PartitionKey,
                AccountType = accountInfo.AccountType,
                AccountNumber = accountInfo.AccountNumber
            };

        public static (bool canCredit, DebitOrCreditFailReason failReason) CanInitiate(this Account account, Money money, bool isContra)
        {
            return isContra switch
            {
                true => account.CanCredit(money),
                _ => account.CanDebit(money)
            };
        }

        public static Entry Initiate(this Account account, DebitOrCreditInfo info, bool isContra)
        {
            return isContra switch
            {
                true => account.Credit(info),
                _ => account.Debit(info)
            };
        }

        public static (bool canCredit, DebitOrCreditFailReason failReason) CanReceive(this Account account, Money money, bool isContra)
        {
            return isContra switch
            {
                true => account.CanDebit(money),
                _ => account.CanCredit(money)
            };
        }

        public static Entry Receive(this Account account, DebitOrCreditInfo info, bool isContra)
        {
            return isContra switch
            {
                true => account.Debit(info),
                _ => account.Credit(info)
            };
        }

        public static Money GetTotal(Money transactionAmount, Money? charge)
            => charge.HasValue ? transactionAmount + charge.Value : transactionAmount;

        internal static Money GetTotal(MoneyInfo transactionAmount, MoneyInfo charge)
        {
            var total = GetTotal(transactionAmount.ToMoney(), charge?.ToMoney());
            return total;
        }
    }
}
