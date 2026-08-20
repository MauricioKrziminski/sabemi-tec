using Microsoft.AspNetCore.Mvc;
using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Infrastructure.Processing;
using Sabemi.Payments.Infrastructure.Queries;

namespace Sabemi.Payments.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder routes)
    {
        var payments = routes.MapGroup("/api/payments").WithTags("Pagamentos");

        payments.MapGet("", ListPaymentsAsync)
            .WithSummary("Lista os eventos recebidos, com filtro por situação e busca por transação ou contrato");

        payments.MapGet("/{id:guid}", GetPaymentAsync)
            .WithSummary("Detalha um evento, incluindo o payload original");

        payments.MapPost("/{id:guid}/reprocess", ReprocessPaymentAsync)
            .WithSummary("Reenfileira um evento que falhou no processamento");

        routes.MapGet("/api/contracts", ListContractsAsync)
            .WithTags("Contratos")
            .WithSummary("Lista a situação consolidada dos contratos");

        routes.MapGet("/api/metrics", GetMetricsAsync)
            .WithTags("Métricas")
            .WithSummary("Resumo para os cartões e para o gráfico do painel");

        return routes;
    }

    private static async Task<IResult> ListPaymentsAsync(
        PaymentQueryService queries,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!TryParseView(status, out var view))
        {
            return TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Filtro inválido",
                Detail = "O filtro de situação aceita: success, error, pending ou processing."
            });
        }

        var query = PaymentQuery.Create(view, search, page, pageSize);
        var result = await queries.ListAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetPaymentAsync(
        Guid id,
        PaymentQueryService queries,
        CancellationToken cancellationToken)
    {
        var payment = await queries.GetAsync(id, cancellationToken);

        return payment is null ? TypedResults.NotFound() : TypedResults.Ok(payment);
    }

    private static async Task<IResult> ReprocessPaymentAsync(
        Guid id,
        PaymentReprocessService reprocess,
        CancellationToken cancellationToken)
    {
        var result = await reprocess.ReprocessAsync(id, cancellationToken);

        return result switch
        {
            ReprocessResult.Requeued => TypedResults.Accepted($"/api/payments/{id}"),
            ReprocessResult.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Reprocessamento indisponível",
                Detail = "Apenas eventos com falha de processamento podem ser reenfileirados."
            })
        };
    }

    private static async Task<IResult> ListContractsAsync(
        PaymentQueryService queries,
        [FromQuery] string? contractId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await queries.ListContractsAsync(
            contractId,
            Math.Max(page ?? 1, 1),
            Math.Clamp(pageSize ?? PaymentQuery.DefaultPageSize, 1, PaymentQuery.MaxPageSize),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetMetricsAsync(
        PaymentQueryService queries,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.GetMetricsAsync(cancellationToken));

    private static bool TryParseView(string? status, out PaymentView? view)
    {
        view = null;

        if (string.IsNullOrWhiteSpace(status) || status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        switch (status.Trim().ToLowerInvariant())
        {
            case "success":
            case "sucesso":
                view = PaymentView.Success;
                return true;
            case "error":
            case "erro":
                view = PaymentView.Error;
                return true;
            case "pending":
            case "pendente":
                view = PaymentView.Pending;
                return true;
            case "processing":
            case "processando":
                view = PaymentView.Processing;
                return true;
            default:
                return false;
        }
    }
}
