using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Core.Domain;
using Sabemi.Payments.Infrastructure.Persistence;

namespace Sabemi.Payments.Infrastructure.Queries;

public sealed class PaymentQueryService(PaymentsDbContext context, TimeProvider timeProvider)
{
    public static readonly TimeSpan SeriesWindow = TimeSpan.FromMinutes(30);

    private const string SettledStatus = "sucesso";

    public async Task<PagedResult<PaymentEventDto>> ListAsync(
        PaymentQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = Filter(context.WebhookEventLogs.AsNoTracking(), query);

        var total = await source.CountAsync(cancellationToken);

        var page = await source
            .OrderByDescending(log => log.ReceivedAt)
            .ThenByDescending(log => log.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = page.Select(PaymentEventDto.From).ToList();

        return new PagedResult<PaymentEventDto>(items, query.Page, query.PageSize, total);
    }

    public async Task<PaymentEventDetailsDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await context.WebhookEventLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);

        if (log is null)
        {
            return null;
        }

        var contract = log.ContractId is null
            ? null
            : await context.ContractStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(entry => entry.ContractId == log.ContractId, cancellationToken);

        return new PaymentEventDetailsDto(
            log.Id,
            log.TransactionId,
            log.ContractId,
            log.Amount,
            log.PaymentDate,
            log.PaymentStatus,
            log.Status,
            PaymentViews.From(log.Status, log.PaymentStatus),
            log.ErrorMessage,
            log.Attempts,
            log.HasPayloadDivergence,
            log.ReceivedAt,
            log.ProcessedAt,
            log.NextAttemptAt,
            log.ProcessingStartedAt,
            log.CorrelationId,
            Parse(log.Payload),
            Parse(log.Headers),
            contract is null ? null : ContractStatusDto.From(contract));
    }

    public async Task<PagedResult<ContractStatusDto>> ListContractsAsync(
        string? contractId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var source = context.ContractStatuses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(contractId))
        {
            source = source.Where(contract => EF.Functions.ILike(contract.ContractId, $"%{contractId.Trim()}%"));
        }

        var total = await source.CountAsync(cancellationToken);

        var contracts = await source
            .OrderByDescending(contract => contract.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = contracts.Select(ContractStatusDto.From).ToList();

        return new PagedResult<ContractStatusDto>(items, page, pageSize, total);
    }

    public async Task<MetricsDto> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var logs = context.WebhookEventLogs.AsNoTracking();

        var totals = await logs
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Processed = group.Count(log => log.Status == EventProcessingStatus.Processed),
                Failures = group.Count(log =>
                    log.Status == EventProcessingStatus.Invalid
                    || log.Status == EventProcessingStatus.Failed
                    || log.Status == EventProcessingStatus.PermanentlyFailed
                    || (log.Status == EventProcessingStatus.Processed && log.PaymentStatus != SettledStatus)),
                InProgress = group.Count(log =>
                    log.Status == EventProcessingStatus.Pending
                    || log.Status == EventProcessingStatus.Processing)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var settled = await context.ContractStatuses
            .AsNoTracking()
            .SumAsync(contract => (decimal?)contract.TotalPaid, cancellationToken) ?? 0m;

        var contracts = await context.ContractStatuses.AsNoTracking().CountAsync(cancellationToken);

        var since = timeProvider.GetUtcNow() - SeriesWindow;

        var grouped = await logs
            .Where(log => log.ReceivedAt >= since)
            .GroupBy(log => new
            {
                log.ReceivedAt.Year,
                log.ReceivedAt.Month,
                log.ReceivedAt.Day,
                log.ReceivedAt.Hour,
                log.ReceivedAt.Minute
            })
            .Select(group => new
            {
                group.Key,
                Total = group.Count(),
                Failures = group.Count(log =>
                    log.Status == EventProcessingStatus.Invalid
                    || log.Status == EventProcessingStatus.Failed
                    || log.Status == EventProcessingStatus.PermanentlyFailed
                    || (log.Status == EventProcessingStatus.Processed && log.PaymentStatus != SettledStatus))
            })
            .ToListAsync(cancellationToken);

        var buckets = grouped
            .Select(bucket => new MetricsBucketDto(
                new DateTimeOffset(
                    bucket.Key.Year,
                    bucket.Key.Month,
                    bucket.Key.Day,
                    bucket.Key.Hour,
                    bucket.Key.Minute,
                    0,
                    TimeSpan.Zero),
                bucket.Total,
                bucket.Failures))
            .OrderBy(bucket => bucket.Minute)
            .ToList();

        return new MetricsDto(
            totals?.Total ?? 0,
            totals?.Processed ?? 0,
            totals?.Failures ?? 0,
            totals?.InProgress ?? 0,
            settled,
            contracts,
            buckets);
    }

    private static IQueryable<WebhookEventLog> Filter(IQueryable<WebhookEventLog> source, PaymentQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.ContractId))
        {
            source = source.Where(log =>
                log.ContractId != null && EF.Functions.ILike(log.ContractId, $"%{query.ContractId}%"));
        }

        return query.View switch
        {
            PaymentView.Success => source.Where(log =>
                log.Status == EventProcessingStatus.Processed && log.PaymentStatus == SettledStatus),

            PaymentView.Error => source.Where(log =>
                log.Status == EventProcessingStatus.Invalid
                || log.Status == EventProcessingStatus.Failed
                || log.Status == EventProcessingStatus.PermanentlyFailed
                || (log.Status == EventProcessingStatus.Processed && log.PaymentStatus != SettledStatus)),

            PaymentView.Pending => source.Where(log => log.Status == EventProcessingStatus.Pending),

            PaymentView.Processing => source.Where(log => log.Status == EventProcessingStatus.Processing),

            _ => source
        };
    }

    private static JsonNode? Parse(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonNode.Parse(json);
}
