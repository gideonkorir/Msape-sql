using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Msape.BookKeeping.Data.EF.Configurations
{
    //split to separate table since I don't need this for normal processing & could be potentially large
    public class TransactionMetadataEntitytTypeConfiguration : IEntityTypeConfiguration<TransactionMetadata>
    {
        public void Configure(EntityTypeBuilder<TransactionMetadata> builder)
        {
            builder.HasKey(c => c.TransactionId);
            builder.Property(c => c.TransactionId).IsRequired().HasColumnName("transaction_id").ValueGeneratedNever();

            builder.Property(c => c.Data).IsUnicode().HasColumnName("data")
                .HasJsonValueConversion();
            builder.HasCheckConstraint("CHK_Transactions_MaxValue", "transaction_id <= 18446744073709551615");
            builder.ToTable("transaction_metadata");
        }
    }
}
