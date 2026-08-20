using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sabemi.Payments.Core.Domain;
using Sabemi.Payments.IntegrationTests.Infrastructure;

namespace Sabemi.Payments.IntegrationTests;

public sealed class DashboardApiTests(PaymentsApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Listagem_filtra_por_situacao_e_por_contrato()
    {
        await SeedAsync();

        var todos = await GetPaymentsAsync(string.Empty);
        Assert.Equal(4, todos.GetProperty("total").GetInt32());

        var comErro = await GetPaymentsAsync("?status=error");
        var idsComErro = TransactionIds(comErro);
        Assert.Equal(2, comErro.GetProperty("total").GetInt32());
        Assert.Contains("TRX-E1", idsComErro);
        Assert.Contains("TRX-E2", idsComErro);

        var comSucesso = await GetPaymentsAsync("?status=sucesso");
        Assert.Equal(2, comSucesso.GetProperty("total").GetInt32());

        var doContrato = await GetPaymentsAsync("?contractId=CT-A");
        Assert.Equal(2, doContrato.GetProperty("total").GetInt32());

        var combinado = await GetPaymentsAsync("?contractId=CT-A&status=error");
        Assert.Equal(1, combinado.GetProperty("total").GetInt32());
        Assert.Equal("TRX-E1", TransactionIds(combinado).Single());
    }

    [Fact]
    public async Task Filtro_de_contrato_aceita_parte_do_identificador_sem_diferenciar_caixa()
    {
        await SeedAsync();

        var emMinusculas = await GetPaymentsAsync("?contractId=ct-b");
        Assert.Equal(2, emMinusculas.GetProperty("total").GetInt32());

        var parcial = await GetPaymentsAsync("?contractId=T-A");
        Assert.Equal(2, parcial.GetProperty("total").GetInt32());

        var semResultado = await GetPaymentsAsync("?contractId=CT-INEXISTENTE");
        Assert.Equal(0, semResultado.GetProperty("total").GetInt32());
        Assert.Equal(0, semResultado.GetProperty("totalPages").GetInt32());
    }

    [Fact]
    public async Task Listagem_pagina_os_resultados()
    {
        await SeedAsync();

        var primeira = await GetPaymentsAsync("?page=1&pageSize=3");
        Assert.Equal(3, primeira.GetProperty("items").GetArrayLength());
        Assert.Equal(2, primeira.GetProperty("totalPages").GetInt32());

        var segunda = await GetPaymentsAsync("?page=2&pageSize=3");
        Assert.Equal(1, segunda.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Filtro_de_situacao_desconhecido_e_recusado()
    {
        var response = await Client.GetAsync("/api/payments?status=qualquer");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Detalhe_devolve_o_payload_original_e_a_situacao_do_contrato()
    {
        await Factory.SendWebhookAsync(Client, Payload("TRX-D1", "CT-D", 15.75m));
        var processed = await Factory.WaitForStatusAsync("TRX-D1", EventProcessingStatus.Processed);

        var detail = await Client.GetFromJsonAsync<JsonElement>($"/api/payments/{processed.Id}");

        Assert.Equal("TRX-D1", detail.GetProperty("transactionId").GetString());
        Assert.Equal("TRX-D1", detail.GetProperty("payload").GetProperty("id_transacao").GetString());
        Assert.Equal(15.75m, detail.GetProperty("contract").GetProperty("totalPaid").GetDecimal());
        Assert.Equal("success", detail.GetProperty("view").GetString());
    }

    [Fact]
    public async Task Metricas_resumem_eventos_contratos_e_valor_liquidado()
    {
        await SeedAsync();

        var metrics = await Client.GetFromJsonAsync<JsonElement>("/api/metrics");

        Assert.Equal(4, metrics.GetProperty("totalEvents").GetInt32());
        Assert.Equal(2, metrics.GetProperty("failures").GetInt32());
        Assert.Equal(300.00m, metrics.GetProperty("totalSettled").GetDecimal());
        Assert.True(metrics.GetProperty("series").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Contratos_listam_a_situacao_consolidada()
    {
        await SeedAsync();

        var contracts = await Client.GetFromJsonAsync<JsonElement>("/api/contracts?contractId=CT-A");

        Assert.Equal(1, contracts.GetProperty("total").GetInt32());
        Assert.Equal(100.00m, contracts.GetProperty("items")[0].GetProperty("totalPaid").GetDecimal());
    }

    private async Task SeedAsync()
    {
        await Factory.SendWebhookAsync(Client, Payload("TRX-S1", "CT-A", 100.00m));
        await Factory.SendWebhookAsync(Client, Payload("TRX-S2", "CT-B", 200.00m));
        await Factory.SendWebhookAsync(Client, Payload("TRX-E1", "CT-A", 30.00m, status: "erro"));
        await Factory.SendWebhookAsync(
            Client,
            """
            {"id_transacao":"TRX-E2","id_contrato":"CT-B","valor":-5,"data_pagamento":"2026-01-05T10:00:00-03:00","status":"sucesso"}
            """);

        await Factory.WaitForStatusAsync("TRX-S1", EventProcessingStatus.Processed);
        await Factory.WaitForStatusAsync("TRX-S2", EventProcessingStatus.Processed);
        await Factory.WaitForStatusAsync("TRX-E1", EventProcessingStatus.Processed);
        await Factory.WaitForStatusAsync("TRX-E2", EventProcessingStatus.Invalid);
    }

    private async Task<JsonElement> GetPaymentsAsync(string query) =>
        await Client.GetFromJsonAsync<JsonElement>($"/api/payments{query}");

    private static List<string?> TransactionIds(JsonElement page) =>
        page.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("transactionId").GetString())
            .ToList();
}
