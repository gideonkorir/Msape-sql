using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Msape.BookKeeping.Data.EF.Configurations
{
    public class EntryEntityTypeConfiguration : IEntityTypeConfiguration<Entry>
    {
        public void Configure(EntityTypeBuilder<Entry> builder)
        {
            builder.HasKey(nameof(Entry.TransactionId), nameof(Entry.AccountId), nameof(Entry.EntryType));
            builder.Property(c => c.TransactionId).HasColumnName("transaction_id").IsRequired();
            builder.Property(c => c.AccountId).HasColumnName("account_id").IsRequired();
            //owned.Property(c => c.Amount).HasColumnType("dest_entry_amount");
            builder.Property(c => c.EntryType).HasColumnName("entry_type").IsRequired();
            builder.Property(c => c.PostedDate).HasColumnName("posted_date").IsRequired();
            builder.OwnsOne(c => c.BalanceAfter, owned =>
            {
                owned.Property(c => c.Value).HasColumnName("balanceafter_value").IsMoney().IsRequired(true);
                owned.Property(c => c.Currency).HasColumnName("balanceafter_currency").IsRequired(true);
            });
            builder.Navigation(c => c.BalanceAfter).IsRequired();
            builder.Property(c => c.IsPlus).HasColumnName("is_plus").IsRequired();
            builder.HasOne(typeof(Transaction))
                .WithMany(nameof(Transaction.Entries))
                .HasForeignKey(nameof(Entry.TransactionId))
                .IsRequired()
                .HasConstraintName("FK_Transaction_Entries_TransactionId")
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(typeof(Account))
                .WithMany()
                .HasForeignKey(nameof(Entry.AccountId))
                .IsRequired()
                .HasConstraintName("FK_Transaction_Entries_AccountId")
                .OnDelete(DeleteBehavior.Cascade);
            builder.ToTable("transaction_entries");
        }
    }
}
