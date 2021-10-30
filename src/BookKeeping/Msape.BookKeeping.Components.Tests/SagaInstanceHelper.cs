using Msape.BookKeeping.Components.Consumers.Posting.Saga;
using Msape.BookKeeping.InternalContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Tests
{
    public static class SagaInstanceHelper
    {
        public static bool HasSameData(SagaAccountInfo sagaAccountInfo, AccountId accountId)
            => sagaAccountInfo.AccountId == accountId.Id
            && string.Equals(sagaAccountInfo.AccountNumber, accountId.AccountNumber, StringComparison.Ordinal)
            && sagaAccountInfo.AccountSubjectId == accountId.SubjectId
            && sagaAccountInfo.AccountType == accountId.AccountType
            && string.Equals(sagaAccountInfo.Name, accountId.Name, StringComparison.Ordinal);
    }
}
