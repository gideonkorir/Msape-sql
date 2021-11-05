using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Data.EF
{
    internal class IdCache
    {
        private readonly BookKeepingContext _bookKeepingContext;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly string _sql;
        private readonly int _rangeSize;
        private ulong _nextValue = 1, _maxValue = 0; //_nextValue is greater than _maxValue to cause us to load values from the db

        public IdCache(BookKeepingContext bookKeepingContext, string sequenceName)
        {
            _bookKeepingContext = bookKeepingContext;
            _sql = GetSql(sequenceName);
            _rangeSize = 50; //start with 50 to improve on this later
        }

        public async Task<ulong> GetValueAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if(_nextValue > _maxValue)
                {
                    using var command = _bookKeepingContext.Database.GetDbConnection().CreateCommand();
                    command.CommandText = _sql;
                    var p = command.CreateParameter();
                    p.ParameterName = "@range_size";
                    p.Value = _rangeSize;
                    p.DbType = System.Data.DbType.Int32;
                    command.Parameters.Add(p);
                    _bookKeepingContext.Database.OpenConnection();
                    using var result = command.ExecuteReader();
                    if (result.Read())
                    {
                        _nextValue = decimal.ToUInt64(result.GetDecimal(0));
                        _maxValue = decimal.ToUInt64(result.GetDecimal(1));
                    }
                }
                var value = _nextValue;
                _nextValue += 1;
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
