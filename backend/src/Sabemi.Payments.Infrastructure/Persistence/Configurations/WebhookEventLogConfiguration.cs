using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabemi.Payments.Core.Domain;

namespace Sabemi.Payments.Infrastructure.Persistence.Configurations;

internal sealed class WebhookEventLogConfiguration : IEntityTypeConfiguration<WebhookEventLog>
{
    public void Configure(EntityTypeBuilder<WebhookEventLog> builder)
    {
        builder.ToTable("webhook_event_logs", table =>
        {
            table.HasCheckConstraint(
                "ck_webhook_event_logs_status",
                "status IN ('Pending', 'Processing', 'Processed', 'Invalid', 'Failed', 'PermanentlyFailed')");

            table.HasCheckConstraint(
                "ck_webhook_event_logs_amount",
                "amount IS NULL OR amount > 0 OR status = 'Invalid'");
        });

        builder.HasKey(log => log.Id);

        builder.Property(log => log.Id).ValueGeneratedNever();

        builder.Property(log => log.TransactionId).HasMaxLength(100).IsRequired();

        builder.Property(log => log.ContractId).HasMaxLength(50);

        builder.Property(log => log.Amount).HasPrecision(18, 2);

        builder.Property(log => log.PaymentStatus).HasMaxLength(20);

        builder.Property(log => log.Payload).HasColumnType("jsonb").IsRequired();

        builder.Property(log => log.PayloadHash).HasColumnType("bytea").IsRequired();

        builder.Property(log => log.Headers).HasColumnType("jsonb");

        builder.Property(log => log.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(log => log.Attempts).HasDefaultValue((short)0);

        builder.Property(log => log.ReceivedAt).HasDefaultValueSql("now()");

        builder.Property(log => log.CorrelationId).HasMaxLength(64);

        builder.Property(log => log.HasPayloadDivergence).HasDefaultValue(false);

        builder.Ignore(log => log.IsTerminal);

        builder.HasIndex(log => log.TransactionId)
            .IsUnique()
            .HasDatabaseName("ux_webhook_event_logs_transaction_id");

        builder.HasIndex(log => log.ReceivedAt)
            .IsDescending()
            .HasDatabaseName("ix_webhook_event_logs_received_at");

        builder.HasIndex(log => new { log.ContractId, log.ReceivedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_webhook_event_logs_contract_id_received_at");

        builder.HasIndex(log => new { log.Status, log.ReceivedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_webhook_event_logs_status_received_at");

        builder.HasIndex(log => log.NextAttemptAt)
            .HasFilter("status IN ('Pending', 'Failed')")
            .HasDatabaseName("ix_webhook_event_logs_next_attempt_at");
    }
}
