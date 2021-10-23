using Msape.BookKeeping.Api.Infra;
using Msape.BookKeeping.InternalContracts;

namespace Msape.BookKeeping.Api.Controllers
{
    internal static class SubjectHelper
    {
        public static AccountId ToAccountId(this CacheableAccountSubject subject)
            => new ()
            {
                AccountNumber = subject.AccountNumber,
                AccountType = subject.AccountType,
                Id = subject.AccountId,
                Name = subject.Name,
                SubjectId = subject.Id
            };
    }
}
