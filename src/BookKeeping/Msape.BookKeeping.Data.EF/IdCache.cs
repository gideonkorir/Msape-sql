using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Data.EF
{
    internal class IdCache
    {
        private readonly BookKeepingContext _bookKeepingContext;
        private readonly List<long> _cache;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly string _sql, _sequenceName;

        public IdCache(BookKeepingContext bookKeepingContext, string sequenceName, int valueCount)
        {
            _bookKeepingContext = bookKeepingContext;
            _cache = new List<long>(valueCount);
            _sequenceName = sequenceName;
            _sql = GetSql(sequenceName, valueCount);
        }

        public async Task<long> GetValueAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if(_cache.Count == 0)
                {
                    using var command = _bookKeepingContext.Database.GetDbConnection().CreateCommand();
                    command.CommandText = _sql;
                    _bookKeepingContext.Database.OpenConnection();
                    using var result = command.ExecuteReader();
                    while (result.Read())
                    {
                        _cache.Add(result.GetInt64(0));
                    }
                }
                var value = _cache[^1];
                _cache.RemoveAt(_cache.Count - 1);
                return value;
            }
            finally
            {
                _semaphore.Release();
            }
        }


        private static string GetSql(string sequenceName, int valueCount)
            => $"WITH iter(num) AS( SELECT 0 UNION ALL SELECT num + 1 FROM iter WHERE num < {valueCount}) SELECT NEXT VALUE FOR {sequenceName} AS [value] FROM (SELECT num FROM iter) t";

    }
}
