using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Msape.BookKeeping.Data.EF.Configurations
{
    public class AccountEntityTypeConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).UseIdentityColumn().IsRequired().HasColumnName("id");
            builder.Property(c => c.PartyId).HasColumnName("party_id").IsRequired();
            builder.Property(c => c.AccountClass).HasColumnName("account_class").IsRequired();
            builder.Property(c => c.AccountStatus).HasColumnName("account_status").IsRequired();
            builder.Property(c => c.AccountType).HasColumnName("account_type").IsRequired();
            builder.Property(c => c.MaxBalance).HasColumnName("max_balance").IsRequired();
            builder.Property(c => c.MinBalance).HasColumnName("min_balance").IsRequired();
            builder.Property(c => c.Version).HasColumnName("version").IsRequired();
            builder.Property(c => c.RowVersion).IsConcurrencyToken().IsRowVersion().HasColumnName("row_version")
                .HasConversion<byte[]>();
            builder.OwnsOne(p => p.Balance, owned =>
            {
                owned.Property(p => p.Value).HasColumnName("balance_value").IsMoney().IsRequired();
                owned.Property(p => p.Currency).HasColumnName("balance_currency").IsRequired();
            });
            builder.Navigation(c => c.Balance).IsRequired();
            builder.Property(c => c.MaxBalance).IsRequired().HasDefaultValue(1_000_000_000M).IsMoney();
            builder.Property(c => c.MinBalance).IsRequired().HasDefaultValue(0).IsMoney();

            builder.ToTable("accounts");
        }
    }
}
