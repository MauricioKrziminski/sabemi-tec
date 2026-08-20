using Microsoft.EntityFrameworkCore;
using Sabemi.Payments.Core.Domain;

namespace Sabemi.Payments.Infrastructure.Persistence;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<WebhookEventLog> WebhookEventLogs => Set<WebhookEventLog>();

    public DbSet<ContractStatus> ContractStatuses => Set<ContractStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}
