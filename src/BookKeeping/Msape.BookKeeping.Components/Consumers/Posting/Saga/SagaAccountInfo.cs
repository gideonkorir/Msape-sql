using Msape.BookKeeping.Data;
using Msape.BookKeeping.InternalContracts;

namespace Msape.BookKeeping.Components.Consumers.Posting.Saga
{
    public class SagaAccountInfo
    {
        public long AccountId { get; init; }
        public long AccountSubjectId { get; init; }
        public string Name { get; init; }
        public string AccountNumber { get; init; }
        public AccountType AccountType { get; init; }

        public static SagaAccountInfo FromAccountId(AccountId accountId)
            => new()
            {
                AccountId = accountId.Id,
                AccountSubjectId = accountId.SubjectId,
                Name = accountId.Name,
                AccountNumber = accountId.AccountNumber,
                AccountType = accountId.AccountType
            };

        public AccountId ToAccountId()
            => new()
            {
                Id = AccountId,
                AccountType = AccountType,
                Name = Name,
                AccountNumber = AccountNumber,
                SubjectId = AccountSubjectId
            };
    }
}
