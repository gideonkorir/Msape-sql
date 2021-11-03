using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Data.EF
{
    public class BookKeepingContext : DbContext
    {
        private static readonly string _txIdSeq = "tx_id_generator";

        private readonly IdCache _txCache;

        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountSubject> Subjects { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<ChargeData> ChargeData { get; set; }

        public BookKeepingContext(DbContextOptions<BookKeepingContext> options)
            : base(options)
        {
            _txCache = new IdCache(this, _txIdSeq, valueCount: 10);
        }

        public async Task<long> NextTransactionIdAsync(CancellationToken cancellationToken)
        {
            var value = await _txCache.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return value;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookKeepingContext).Assembly);
            modelBuilder.HasSequence(_txIdSeq);
        }
    }
}
