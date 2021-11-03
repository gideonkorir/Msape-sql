using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Msape.BookKeeping.Data.EF.Configurations
{
    public class ChargeConfigurationEntityTypeConfiguration : IEntityTypeConfiguration<ChargeConfiguration>
    {
        public void Configure(EntityTypeBuilder<ChargeConfiguration> builder)
        {
            builder.HasKey(nameof(ChargeConfiguration.TransactionType), nameof(ChargeConfiguration.Currency));
            builder.Property(c => c.TransactionType).HasColumnName("transaction_type").IsRequired();
            builder.Property(c => c.Currency).HasColumnName("currency").IsRequired();
            builder.HasMany(c => c.Data)
                .WithOne()
                .HasForeignKey("FK_ChargeData_Configuration")
                .OnDelete(DeleteBehavior.Cascade);
            builder.ToTable("charge_configurations");
        }
    }

    public class ChargeDataEntityTypeConfiguration : IEntityTypeConfiguration<ChargeData>
    {
        public void Configure(EntityTypeBuilder<ChargeData> builder)
        {
            builder.Property<TransactionType>(nameof(ChargeConfiguration.TransactionType));
            builder.Property<Currency>(nameof(ChargeConfiguration.Currency));
            builder.Property(c => c.FromDate).IsRequired()
                .HasDefaultValueSql("getutcdate()")
                .HasColumnName("from_date");
            builder.Property(c => c.ToDate).HasColumnName("to_date");
            builder.HasKey(
                nameof(ChargeConfiguration.TransactionType),
                nameof(ChargeConfiguration.Currency),
                nameof(ChargeData.FromDate)
                );
            builder.ToTable("charge_configuration_data");
        }
    }
}
