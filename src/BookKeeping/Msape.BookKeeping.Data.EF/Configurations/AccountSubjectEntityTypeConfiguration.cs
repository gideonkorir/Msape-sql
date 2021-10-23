using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Msape.BookKeeping.Data.EF.Configurations
{
    public class AccountSubjectEntityTypeConfiguration : IEntityTypeConfiguration<AccountSubject>
    {
        public void Configure(EntityTypeBuilder<AccountSubject> builder)
        {
            builder.HasKey(c => c.Id).IsClustered(false);
            builder.Property(c => c.Id).UseIdentityColumn().IsRequired().HasColumnName("id");
            builder.Property(c => c.AccountNumber).IsUnicode().IsRequired().HasMaxLength(50).IsRequired()
                .HasColumnName("account_number");
            builder.Property(c => c.AccountType).IsRequired().HasColumnName("account_type");
            builder.HasIndex(nameof(AccountSubject.AccountNumber), nameof(AccountSubject.AccountType))
                .IsClustered()
                .IsUnique()
                .HasDatabaseName("UN_AccountSubject_AccountNumber_AccountType");
            builder.Property(c => c.Name).IsUnicode().IsRequired().HasMaxLength(100).HasColumnName("name");
            builder.Property(c => c.DateCreatedUtc).HasDefaultValueSql("getutcdate()").HasColumnName("date_created_utc");

            //account configuration
            builder.Property<long>("AccountId").HasColumnName("account_id").IsRequired();
            builder.HasOne(p => p.Account)
                .WithMany()
                .HasForeignKey("AccountId")
                .HasConstraintName("FK_AccountSubjects_AccountId");

            builder.ToTable("account_subjects");
        }
    }
}
