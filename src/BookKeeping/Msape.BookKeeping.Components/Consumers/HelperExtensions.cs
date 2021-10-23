using Msape.BookKeeping.Components.Consumers.Posting;
using Msape.BookKeeping.Components.Consumers.Posting.Saga;
using Msape.BookKeeping.Data;
using Msape.BookKeeping.InternalContracts;

namespace Msape.BookKeeping.Components.Consumers
{
    internal static class HelperExtensions
    {
        public static SagaAccountInfo ToSagaAccountInfo(this AccountId accountId)
            => new()
            {
                AccountId = accountId.Id,
                AccountSubjectId = accountId.SubjectId,
                Name = accountId.Name,
                AccountType = accountId.AccountType,
                AccountNumber = accountId.AccountNumber
            };

        public static AccountId ToAccountId(this SagaAccountInfo accountInfo)
            => new()
            {
                Id = accountInfo.AccountId,
                SubjectId = accountInfo.AccountSubjectId,
                Name = accountInfo.Name,
                AccountNumber = accountInfo.AccountNumber,
                AccountType = accountInfo.AccountType
            };
    }
}
