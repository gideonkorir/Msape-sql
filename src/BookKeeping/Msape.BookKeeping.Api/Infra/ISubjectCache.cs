using Microsoft.Extensions.Caching;
using Msape.BookKeeping.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Infra
{
    public interface ISubjectCache
    {
        Task<CacheableAccountSubject> GetSubjectAsync(string accountNumber, AccountType accountType, CancellationToken cancellationToken);
    }

    public record CacheableAccountSubject
    {
        public long Id { get; init; }
        public long AccountId { get; init; }
        public string Name { get; init; }
        public string AccountNumber { get; init; }
        public AccountType AccountType { get; init; }
    }
}
