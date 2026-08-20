using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Sabemi.Payments.Core.Security;

namespace Sabemi.Payments.Api.Security;

/// <summary>
/// Autentica a requisição antes de qualquer desserialização.
///
/// Ler o corpo bruto antes de autenticar é inerente ao HMAC, então a superfície exposta é
/// limitada aqui mesmo: tipo de conteúdo e tamanho são verificados antes da leitura.
/// </summary>
internal sealed class WebhookSignatureFilter(
    WebhookSignatureValidator validator,
    IOptions<WebhookSignatureOptions> options,
    ILogger<WebhookSignatureFilter> logger) : IEndpointFilter
{
    /// <summary>Chave usada para repassar o corpo bruto ao handler sem lê-lo duas vezes.</summary>
    internal const string RawBodyKey = "webhook:raw-body";

    internal const int MaxBodyBytes = 64 * 1024;

    private readonly WebhookSignatureOptions _options = options.Value;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;

        if (!IsJson(request.ContentType))
        {
            return TypedResults.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        if (request.ContentLength > MaxBodyBytes)
        {
            return TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        // Protege também o caso de envio em chunks, quando não há Content-Length declarado.
        if (context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } sizeFeature)
        {
            sizeFeature.MaxRequestBodySize = MaxBodyBytes;
        }

        string body;
        try
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync(context.HttpContext.RequestAborted);
            request.Body.Position = 0;
        }
        catch (BadHttpRequestException)
        {
            return TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var result = validator.Validate(
            body,
            request.Headers[_options.SignatureHeader],
            request.Headers[_options.TimestampHeader]);

        if (!result.IsValid)
        {
            logger.LogWarning(
                "Webhook rejeitado na autenticação. Motivo: {Reason}. Origem: {RemoteIp}",
                result.Reason,
                context.HttpContext.Connection.RemoteIpAddress);

            // A resposta não detalha o motivo, para não ajudar quem está tentando forjar a assinatura.
            return TypedResults.Unauthorized();
        }

        context.HttpContext.Items[RawBodyKey] = body;

        return await next(context);
    }

    private static bool IsJson(string? contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out var parsed)
        && string.Equals(parsed.MediaType, "application/json", StringComparison.OrdinalIgnoreCase);
}
