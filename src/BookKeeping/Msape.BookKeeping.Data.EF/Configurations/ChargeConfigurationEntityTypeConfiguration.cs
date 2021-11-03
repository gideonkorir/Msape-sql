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
                .IsRequired()
                .HasForeignKey("FK_ChargeData_Configuration")
                .OnDelete(DeleteBehavior.Cascade);
            builder.ToTable("charge_configurations");
        }
    }
}
