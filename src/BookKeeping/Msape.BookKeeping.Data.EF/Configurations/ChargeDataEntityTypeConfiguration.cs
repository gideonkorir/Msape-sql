using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Msape.BookKeeping.Data.EF.Configurations
{
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
            builder.HasQueryFilter(c => c.ToDate == null);
            builder.ToTable("charge_configuration_data");
        }
    }
}
