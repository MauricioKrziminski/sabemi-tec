using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Sabemi.Payments.Api.Security;
using Sabemi.Payments.Core.Contracts;
using Sabemi.Payments.Infrastructure.Ingestion;

namespace Sabemi.Payments.Api.Endpoints;

public static class WebhookEndpoints
{
    /// <summary>Headers preservados no log. A assinatura fica de fora de propósito.</summary>
    private static readonly string[] AuditedHeaders =
    [
        "User-Agent",
        "X-Timestamp",
        "X-Correlation-Id",
        "X-Request-Id"
    ];

    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/webhooks").WithTags("Webhooks");

        group.MapPost("/pagamento", ReceivePaymentAsync)
            .AddEndpointFilter<WebhookSignatureFilter>()
            .WithSummary("Recebe notificações de pagamento do banco parceiro")
            .WithDescription(
                "Autentica a requisição por assinatura HMAC-SHA256, registra o evento bruto e " +
                "devolve o controle imediatamente. A regra de negócio roda em background.")
            .Produces<WebhookReceiptResponse>(StatusCodes.Status202Accepted)
            .Produces<WebhookReceiptResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return routes;
    }

    private static async Task<IResult> ReceivePaymentAsync(
        HttpContext context,
        WebhookIngestionService ingestion,
        CancellationToken cancellationToken)
    {
        // O corpo já foi lido e autenticado pelo filtro de assinatura.
        var rawBody = (string)context.Items[WebhookSignatureFilter.RawBodyKey]!;

        var request = new WebhookIngestionRequest(
            rawBody,
            CaptureHeaders(context.Request),
            ResolveCorrelationId(context));

        var outcome = await ingestion.IngestAsync(request, cancellationToken);

        return outcome switch
        {
            IngestionOutcome.Accepted accepted => TypedResults.Accepted(
                $"/api/payments/{accepted.EventId}",
                new WebhookReceiptResponse(accepted.EventId, "Pending", false)),

            // Reenvio é sucesso idempotente, não erro. Responder 409 faria o parceiro
            // acionar a operação dele sem necessidade.
            IngestionOutcome.Duplicate duplicate => TypedResults.Ok(
                new WebhookReceiptResponse(duplicate.EventId, duplicate.Status.ToString(), true)),

            // O evento fica registrado e visível no painel, mas o parceiro recebe a recusa.
            IngestionOutcome.Rejected rejected => TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Payload recusado na validação",
                Detail = "O evento foi registrado para auditoria, porém não será processado.",
                Extensions =
                {
                    ["eventId"] = rejected.EventId,
                    ["errors"] = rejected.Errors
                }
            }),

            IngestionOutcome.Unparseable unparseable => TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Corpo da requisição inválido",
                Detail = unparseable.Message
            }),

            _ => TypedResults.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private static string ResolveCorrelationId(HttpContext context) =>
        context.Request.Headers["X-Correlation-Id"].FirstOrDefault() is { Length: > 0 } correlationId
            ? correlationId
            : context.TraceIdentifier;

    private static string CaptureHeaders(HttpRequest request)
    {
        var headers = new JsonObject();

        foreach (var name in AuditedHeaders)
        {
            if (request.Headers[name].FirstOrDefault() is { Length: > 0 } value)
            {
                headers[name] = value;
            }
        }

        return headers.ToJsonString(JsonSerializerOptions.Web);
    }
}

/// <summary>Confirmação devolvida ao banco parceiro.</summary>
/// <param name="Id">Identificador do evento registrado.</param>
/// <param name="Status">Situação atual do processamento.</param>
/// <param name="Duplicated">Verdadeiro quando o id de transação já havia sido recebido.</param>
public sealed record WebhookReceiptResponse(Guid Id, string Status, bool Duplicated);
