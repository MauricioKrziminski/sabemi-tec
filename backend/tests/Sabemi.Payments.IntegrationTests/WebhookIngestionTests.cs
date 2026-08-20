using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sabemi.Payments.Core.Domain;
using Sabemi.Payments.IntegrationTests.Infrastructure;

namespace Sabemi.Payments.IntegrationTests;

public sealed class WebhookIngestionTests(PaymentsApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Evento_valido_e_aceito_e_processado_em_background()
    {
        var paidAt = DateTimeOffset.UtcNow.AddHours(-1);

        var response = await Factory.SendWebhookAsync(Client, Payload("TRX-100", amount: 1240.00m, paidAt: paidAt));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var receipt = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(receipt.GetProperty("duplicated").GetBoolean());
        Assert.Equal($"/api/payments/{receipt.GetProperty("id").GetGuid()}", response.Headers.Location?.ToString());

        var processed = await Factory.WaitForStatusAsync("TRX-100", EventProcessingStatus.Processed);
        Assert.Equal(1, processed.Attempts);
        Assert.NotNull(processed.ProcessedAt);

        var contract = await Factory.FindContractAsync("CT-1029");
        Assert.NotNull(contract);
        Assert.Equal(1240.00m, contract.TotalPaid);
        Assert.Equal(1, contract.PaymentCount);
        Assert.Equal("sucesso", contract.LastStatus);
        Assert.Equal("TRX-100", contract.LastTransactionId);
    }

    /// <summary>
    /// Este é o teste que prova a idempotência de efeito, e não apenas a de registro:
    /// o total do contrato não pode dobrar quando o parceiro reenvia a notificação.
    /// </summary>
    [Fact]
    public async Task Reenvio_do_mesmo_id_de_transacao_nao_processa_duas_vezes()
    {
        var body = Payload("TRX-200", amount: 500.00m);

        var first = await Factory.SendWebhookAsync(Client, body);
        await Factory.WaitForStatusAsync("TRX-200", EventProcessingStatus.Processed);

        var second = await Factory.SendWebhookAsync(Client, body);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var receipt = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(receipt.GetProperty("duplicated").GetBoolean());

        await using var context = await Factory.CreateDbContextAsync();
        Assert.Equal(1, await context.WebhookEventLogs.CountAsync(log => log.TransactionId == "TRX-200"));

        var contract = await Factory.FindContractAsync("CT-1029");
        Assert.Equal(500.00m, contract!.TotalPaid);
        Assert.Equal(1, contract.PaymentCount);
    }

    /// <summary>
    /// Corrida real entre notificações simultâneas: quem resolve é o índice único, e a API
    /// não pode devolver erro de servidor por causa disso.
    /// </summary>
    [Fact]
    public async Task Notificacoes_simultaneas_geram_um_unico_registro()
    {
        var body = Payload("TRX-300", amount: 90.00m);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var responses = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => Factory.SendWebhookAsync(Client, body, timestamp)));

        Assert.All(responses, response => Assert.True(
            response.StatusCode is HttpStatusCode.Accepted or HttpStatusCode.OK,
            $"Status inesperado: {(int)response.StatusCode}."));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Accepted);

        await Factory.WaitForStatusAsync("TRX-300", EventProcessingStatus.Processed);

        await using var context = await Factory.CreateDbContextAsync();
        Assert.Equal(1, await context.WebhookEventLogs.CountAsync(log => log.TransactionId == "TRX-300"));

        var contract = await Factory.FindContractAsync("CT-1029");
        Assert.Equal(90.00m, contract!.TotalPaid);
    }

    [Fact]
    public async Task Reenvio_com_corpo_diferente_e_sinalizado()
    {
        await Factory.SendWebhookAsync(Client, Payload("TRX-350", amount: 10.00m));
        await Factory.WaitForStatusAsync("TRX-350", EventProcessingStatus.Processed);

        var response = await Factory.SendWebhookAsync(Client, Payload("TRX-350", amount: 999.00m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var eventLog = await Factory.FindEventAsync("TRX-350");
        Assert.True(eventLog!.HasPayloadDivergence);

        var contract = await Factory.FindContractAsync("CT-1029");
        Assert.Equal(10.00m, contract!.TotalPaid);
    }

    [Fact]
    public async Task Assinatura_invalida_nao_persiste_nada()
    {
        var response = await Factory.SendWebhookAsync(Client, Payload("TRX-400"), secret: "segredo-errado");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await using var context = await Factory.CreateDbContextAsync();
        Assert.Equal(0, await context.WebhookEventLogs.CountAsync());
    }

    [Fact]
    public async Task Carimbo_de_tempo_antigo_e_recusado_como_replay()
    {
        var expired = DateTimeOffset.UtcNow.AddMinutes(-30).ToUnixTimeSeconds();

        var response = await Factory.SendWebhookAsync(Client, Payload("TRX-450"), expired);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Um payload reprovado precisa continuar visível no painel, com a mensagem que explica a
    /// recusa, e não pode tocar na situação do contrato.
    /// </summary>
    [Fact]
    public async Task Payload_invalido_e_registrado_com_alerta_e_nao_altera_o_contrato()
    {
        var body = """
                   {"id_transacao":"TRX-500","id_contrato":"CT-0140","valor":0,"data_pagamento":"2026-01-10T10:00:00-03:00","status":"pago"}
                   """;

        var response = await Factory.SendWebhookAsync(Client, body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = problem.GetProperty("errors").EnumerateArray().Select(error => error.GetString()).ToList();
        Assert.Contains("O campo valor deve ser maior que zero.", errors);
        Assert.Contains("O campo status deve ser 'sucesso' ou 'erro'.", errors);

        var eventLog = await Factory.FindEventAsync("TRX-500");
        Assert.Equal(EventProcessingStatus.Invalid, eventLog!.Status);
        Assert.Equal(0m, eventLog.Amount);
        Assert.NotNull(eventLog.ErrorMessage);

        Assert.Null(await Factory.FindContractAsync("CT-0140"));
    }

    [Fact]
    public async Task Corpo_que_nao_e_json_e_recusado_sem_registro()
    {
        var response = await Factory.SendWebhookAsync(Client, "isto nao e json");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var context = await Factory.CreateDbContextAsync();
        Assert.Equal(0, await context.WebhookEventLogs.CountAsync());
    }

    [Fact]
    public async Task Tipo_de_conteudo_diferente_de_json_e_recusado()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/pagamento")
        {
            Content = new StringContent("texto", Encoding.UTF8)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Corpo_acima_do_limite_e_recusado()
    {
        var oversized = $$"""{"id_transacao":"TRX-600","observacao":"{{new string('x', 70_000)}}"}""";

        var response = await Factory.SendWebhookAsync(Client, oversized);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    /// <summary>
    /// Pagamento recusado pelo banco é um evento processado com sucesso do nosso lado, mas não
    /// entra no total liquidado do contrato.
    /// </summary>
    [Fact]
    public async Task Pagamento_recusado_registra_o_contrato_sem_somar_no_total()
    {
        await Factory.SendWebhookAsync(Client, Payload("TRX-700", "CT-0771", 320.00m, status: "erro"));

        await Factory.WaitForStatusAsync("TRX-700", EventProcessingStatus.Processed);

        var contract = await Factory.FindContractAsync("CT-0771");
        Assert.Equal(0m, contract!.TotalPaid);
        Assert.Equal(0, contract.PaymentCount);
        Assert.Equal("erro", contract.LastStatus);
    }
}
