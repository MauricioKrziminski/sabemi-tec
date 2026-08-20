using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabemi.Payments.Core.Domain;

namespace Sabemi.Payments.Infrastructure.Persistence.Configurations;

internal sealed class ContractStatusConfiguration : IEntityTypeConfiguration<ContractStatus>
{
    public void Configure(EntityTypeBuilder<ContractStatus> builder)
    {
        builder.ToTable("contract_statuses");

        builder.HasKey(contract => contract.ContractId);

        builder.Property(contract => contract.ContractId).HasMaxLength(50).ValueGeneratedNever();

        builder.Property(contract => contract.LastStatus).HasMaxLength(20).IsRequired();

        builder.Property(contract => contract.LastTransactionId).HasMaxLength(100).IsRequired();

        builder.Property(contract => contract.TotalPaid).HasPrecision(18, 2).HasDefaultValue(0m);

        builder.Property(contract => contract.PaymentCount).HasDefaultValue(0);

        builder.Property(contract => contract.CreatedAt).HasDefaultValueSql("now()");

        builder.Property(contract => contract.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(contract => contract.UpdatedAt)
            .IsDescending()
            .HasDatabaseName("ix_contract_statuses_updated_at");

        builder.HasIndex(contract => contract.LastStatus)
            .HasDatabaseName("ix_contract_statuses_last_status");
    }
}
