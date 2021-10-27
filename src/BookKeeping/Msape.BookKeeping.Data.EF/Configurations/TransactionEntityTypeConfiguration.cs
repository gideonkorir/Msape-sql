using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Msape.BookKeeping.Data.EF.Configurations
{
    public class TransactionEntityTypeConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).IsRequired().HasColumnName("id").ValueGeneratedNever();
            builder.Property(c => c.ParentId).HasColumnName("parent_id");
            builder.OwnsOne(p => p.Amount, owned =>
            {
                owned.Property(c => c.Currency).HasColumnName("amount_currency").IsRequired();
                owned.Property(c => c.Value).HasColumnName("amount_value").IsRequired().IsMoney();
            });
            builder.Property(c => c.Timestamp).IsRequired().HasColumnName("timestamp");
            builder.Property(c => c.Status).IsRequired().HasColumnName("status");
            builder.Property(c => c.TransactionType).IsRequired().HasColumnName("transaction_type");
            builder.Property(c => c.IsContra).IsRequired().HasColumnName("is_contra");
            builder.Property(c => c.Notes).HasColumnName("notes").HasMaxLength(256).IsUnicode();
            builder.Property(c => c.FailReason).HasColumnName("transaction_fail_reason").IsRequired();
            builder.Property(c => c.SourceFailReason).HasColumnName("src_fail_reason");
            builder.Property(c => c.DestFailReason).HasColumnName("dest_fail_reason");
            builder.Property(c => c.DateCompleted).HasColumnName("date_completed");
            builder.Property(c => c.RowVersion).IsConcurrencyToken().IsRowVersion().HasColumnName("row_version")
                .HasConversion<byte[]>();
            builder.OwnsOne(p => p.SourceAccount, owned =>
            {
                owned.Property(c => c.AccountId).HasColumnName("from_accountid").IsRequired();
                owned.Property(c => c.AccountSubjectId).HasColumnName("from_accountsubjectid").IsRequired();
                //account as supplied by user information
                owned.HasOne(typeof(Account))
                    .WithMany()
                    .HasForeignKey(nameof(TransactionAccountInfo.AccountId))
                    .HasPrincipalKey(nameof(Account.Id))
                    .HasConstraintName("FK_Transactions_FromAccountId")
                    .OnDelete(DeleteBehavior.Restrict);
                owned.HasOne(typeof(AccountSubject))
                    .WithMany()
                    .HasForeignKey(nameof(TransactionAccountInfo.AccountSubjectId))
                    .HasPrincipalKey(nameof(AccountSubject.Id))
                    .HasConstraintName("FK_Transactions_FromAccountSubjectId")
                    .OnDelete(DeleteBehavior.Restrict);

            });
            builder.OwnsOne(p => p.DestAccount, owned =>
            {
                owned.Property(c => c.AccountId).HasColumnName("to_accountid").IsRequired();
                owned.Property(c => c.AccountSubjectId).HasColumnName("to_accountsubjectid").IsRequired();
                owned.HasOne(typeof(Account))
                    .WithMany()
                    .HasForeignKey(nameof(TransactionAccountInfo.AccountId))
                    .HasPrincipalKey(nameof(Account.Id))
                    .HasConstraintName("FK_Transactions_ToAccountId")
                    .OnDelete(DeleteBehavior.Restrict);
                owned.HasOne(typeof(AccountSubject))
                    .WithMany()
                    .HasForeignKey(nameof(TransactionAccountInfo.AccountSubjectId))
                    .HasPrincipalKey(nameof(AccountSubject.Id))
                    .HasConstraintName("FK_Transactions_ToAccountSubjectId")
                    .OnDelete(DeleteBehavior.NoAction);

            });
            builder.Navigation(c => c.Amount).IsRequired();
            builder.Navigation(c => c.SourceAccount).IsRequired();
            builder.Navigation(c => c.DestAccount).IsRequired();
            builder.HasMany(c => c.Charges)
                .WithOne()
                .HasForeignKey(c => c.ParentId)
                .HasConstraintName("FK_Transactions_ParentId")
                .OnDelete(DeleteBehavior.NoAction);
            //entry information
            //builder.OwnsOne(p => p.Entries, owned =>
            //{
            //    owned.Property(c => c.AccountId).HasColumnName("src_entry_account_id");
            //    //owned.Property(c => c.Amount).HasColumnType("src_entry_amount");
            //    owned.Property(c => c.EntryType).HasColumnName("src_entry_type");
            //    owned.Property(c => c.PostedDate).HasColumnName("src_entry_posted_date");
            //    owned.OwnsOne(c => c.BalanceAfter, owned =>
            //    {
            //        owned.Property(c => c.Value).HasColumnName("src_entry_balanceafter_value").IsMoney();
            //        owned.Property(c => c.Currency).HasColumnName("src_entry_balanceafter_currency");
            //    });
            //});
            //builder.OwnsOne(p => p.DestEntry, owned =>
            //{
            //    owned.Property(c => c.AccountId).HasColumnName("dest_entry_account_id");
            //    //owned.Property(c => c.Amount).HasColumnType("dest_entry_amount");
            //    owned.Property(c => c.EntryType).HasColumnName("dest_entry_type");
            //    owned.Property(c => c.PostedDate).HasColumnName("dest_entry_posted_date");
            //    owned.OwnsOne(c => c.BalanceAfter, owned =>
            //    {
            //        owned.Property(c => c.Value).HasColumnName("dest_entry_balanceafter_value").IsMoney();
            //        owned.Property(c => c.Currency).HasColumnName("dest_entry_balanceafter_currency");
            //    });
            //});
            builder.ToTable("transactions");
        }
    }
}
