using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Data.EF
{
    public class BookKeepingContext : DbContext
    {
        private static readonly string _txIdSeq = "transaction_id_generator";

        private readonly IdCache _txCache;

        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountSubject> Subjects { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<ChargeData> ChargeData { get; set; }

        public BookKeepingContext(DbContextOptions<BookKeepingContext> options)
            : base(options)
        {
            _txCache = new IdCache(this, _txIdSeq);
        }

        public async Task<ulong> NextTransactionIdAsync(CancellationToken cancellationToken)
        {
            var value = await _txCache.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return value;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookKeepingContext).Assembly);
        }

        //Create custom objects when creating db. Don't call in user code.
        //Anything added should be idempotent
        public async Task CreateCustomObjects(CancellationToken cancellation)
        {
            if (Database.IsSqlServer())
            {
                var sql = @$"if not exists(select * from sys.sequences where name = '{_txIdSeq}')
                begin
	                create sequence {_txIdSeq} as decimal(20, 0)
	                start with 1
	                increment by 1
	                minvalue 0
	                maxvalue 18446744073709551615
                end";

                await Database.ExecuteSqlRawAsync(sql, cancellation).ConfigureAwait(false);
            }
        }

    }
}
