using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Msape.BookKeeping.Data.EF.Configurations
{
    public class ChargeDataEntityTypeConfiguration : IEntityTypeConfiguration<ChargeData>
    {
        public void Configure(EntityTypeBuilder<ChargeData> builder)
        {

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("id");
            builder.Property<TransactionType>(nameof(ChargeConfiguration.TransactionType)).HasColumnName("transaction_type");
            builder.Property<Currency>(nameof(ChargeConfiguration.Currency)).HasColumnName("currency");
            builder.Property(c => c.ChargeTransactionType).HasColumnName("charge_transaction_type");
            builder.Property(c => c.ChargeType).IsRequired().HasColumnName("charge_type");
            builder.Property(c => c.FromDate).IsRequired()
                .HasDefaultValueSql("getutcdate()")
                .HasColumnName("from_date");
            builder.Property(c => c.ToDate).HasColumnName("to_date");
            builder.Property(c => c.MinAmount).HasColumnName("min_amount").IsRequired().IsMoney();
            builder.Property(c => c.MaxAmount).HasColumnName("max_amount").IsRequired().IsMoney();
            builder.Property(c => c.ChargeAmount).HasColumnName("charge_amount").IsRequired().IsMoney();
            builder.HasQueryFilter(c => c.ToDate == null);

            builder.HasIndex(
                nameof(ChargeConfiguration.TransactionType),
                nameof(ChargeConfiguration.Currency),
                nameof(ChargeData.ChargeTransactionType),
                nameof(ChargeData.ChargeType),
                nameof(ChargeData.MinAmount),
                nameof(ChargeData.MaxAmount))
                .IsUnique()
                .HasFilter("to_date is null")
                .HasDatabaseName("UN_ChargeData_ChargeType_Amount");

            builder.ToTable("charge_configuration_data");
        }
    }
}
