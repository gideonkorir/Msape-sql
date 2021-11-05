using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Data.EF
{
    internal class IdCache
    {
        private readonly BookKeepingContext _bookKeepingContext;
        private readonly List<ulong> _cache;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly string _sql, _sequenceName;

        public IdCache(BookKeepingContext bookKeepingContext, string sequenceName, int valueCount)
        {
            _bookKeepingContext = bookKeepingContext;
            _cache = new List<ulong>(valueCount);
            _sequenceName = sequenceName;
            _sql = GetSql(sequenceName);
        }

        public async Task<ulong> GetValueAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if(_cache.Count == 0)
                {
                    using var command = _bookKeepingContext.Database.GetDbConnection().CreateCommand();
                    command.CommandText = _sql;
                    var p = command.CreateParameter();
                    p.ParameterName = "@range_size";
                    p.Value = _cache.Count;
                    p.DbType = System.Data.DbType.Int32;

                    command.Parameters.Add(p);
                    _bookKeepingContext.Database.OpenConnection();
                    using var result = command.ExecuteReader();
                    if (result.Read())
                    {
                        var start = decimal.ToUInt64(result.GetDecimal(0));
                        var end = decimal.ToUInt64(result.GetDecimal(1));
                        for (; start <= end; start++)
                            _cache.Add(start);

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


        private static string GetSql(string sequenceName)
            => $@"
DECLARE @range_start AS SQL_VARIANT, @range_end AS SQL_VARIANT;

EXEC sys.sp_sequence_get_range @sequence_name = N'{sequenceName}', @range_size = @range_size, @range_first_value = @range_start OUTPUT, @range_last_value = @range_end OUTPUT;

select CONVERT(DECIMAL(20, 0), @range_start) as range_start, CONVERT(DECIMAL(20,0), @range_end) AS range_end
";

    }
}
