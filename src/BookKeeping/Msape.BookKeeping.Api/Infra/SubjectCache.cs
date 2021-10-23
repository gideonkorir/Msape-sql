using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Msape.BookKeeping.Data;
using Msape.BookKeeping.Data.EF;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Infra
{
    public class SubjectCache : ISubjectCache
    {
        private readonly static JsonSerializerOptions _jsonSerializerOptions = new ()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly IDistributedCache _cache;
        private readonly BookKeepingContext _context;

        public SubjectCache(IDistributedCache cache, BookKeepingContext context)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<CacheableAccountSubject> GetSubjectAsync(string accountNumber, AccountType accountType, CancellationToken cancellationToken)
        {
            var key = $"{accountNumber.ToUpperInvariant()}/{(int)accountType}";
            var item = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
            if(item is not null)
            {
                return JsonSerializer.Deserialize<CacheableAccountSubject>(item, _jsonSerializerOptions);
            }
            var saved = await LoadAsync(accountNumber, accountType, cancellationToken).ConfigureAwait(false);
            var json = JsonSerializer.SerializeToUtf8Bytes(saved, _jsonSerializerOptions);
            await _cache.SetAsync(key, json, cancellationToken).ConfigureAwait(false);
            return saved;
        }

        private async Task<CacheableAccountSubject> LoadAsync(string accountNumber, AccountType accountType, CancellationToken cancellationToken)
        {
            var subject = await _context.Subjects
                .AsNoTracking()
                .Select(c => new CacheableAccountSubject()
                {
                    Id = c.Id,
                    AccountId = c.Account.Id,
                    AccountNumber = c.AccountNumber,
                    AccountType = c.AccountType,
                    Name = c.Name
                })
                .SingleOrDefaultAsync(c => c.AccountNumber == accountNumber && c.AccountType == accountType, cancellationToken)
                .ConfigureAwait(false);
            return subject;
        }
    }
}
