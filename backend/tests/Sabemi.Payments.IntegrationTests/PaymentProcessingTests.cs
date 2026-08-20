using System.Security.Cryptography;
using System.Text;
using Sabemi.Payments.Core.Domain;
using Sabemi.Payments.IntegrationTests.Infrastructure;

namespace Sabemi.Payments.IntegrationTests;

public sealed class PaymentProcessingTests(PaymentsApiFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>
    /// Prova que a fila de verdade é a tabela, e não o canal em memória: um evento inserido
    /// direto no banco, sem passar pelo endpoint, é encontrado e processado pela varredura.
    /// </summary>
    [Fact]
    public async Task Evento_pendente_no_banco_e_recuperado_sem_passar_pelo_endpoint()
    {
        var payload = Payload("TRX-800", "CT-9000", 75.25m);

        await using (var context = await Factory.CreateDbContextAsync())
        {
            context.WebhookEventLogs.Add(new WebhookEventLog
            {
                TransactionId = "TRX-800",
                ContractId = "CT-9000",
                Amount = 75.25m,
                PaymentDate = DateTimeOffset.UtcNow.AddHours(-2),
                PaymentStatus = "sucesso",
                Payload = payload,
                PayloadHash = SHA256.HashData(Encoding.UTF8.GetBytes(payload)),
                Status = EventProcessingStatus.Pending,
                ReceivedAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync();
        }

        await Factory.WaitForStatusAsync("TRX-800", EventProcessingStatus.Processed);

        var contract = await Factory.FindContractAsync("CT-9000");
        Assert.Equal(75.25m, contract!.TotalPaid);
    }

    /// <summary>
    /// O banco parceiro pode reenviar uma notificação antiga depois de uma nova. Os acumuladores
    /// somam sempre, mas a situação atual do contrato continua sendo a do pagamento mais recente.
    /// </summary>
    [Fact]
    public async Task Evento_fora_de_ordem_soma_no_total_sem_regredir_a_situacao_do_contrato()
    {
        var recent = DateTimeOffset.UtcNow.AddHours(-1);
        var older = DateTimeOffset.UtcNow.AddDays(-10);

        await Factory.SendWebhookAsync(Client, Payload("TRX-900", "CT-5000", 200.00m, recent));
        await Factory.WaitForStatusAsync("TRX-900", EventProcessingStatus.Processed);

        await Factory.SendWebhookAsync(Client, Payload("TRX-899", "CT-5000", 50.00m, older, "erro"));
        await Factory.WaitForStatusAsync("TRX-899", EventProcessingStatus.Processed);

        var contract = await Factory.FindContractAsync("CT-5000");
        Assert.Equal(200.00m, contract!.TotalPaid);
        Assert.Equal(1, contract.PaymentCount);
        Assert.Equal("sucesso", contract.LastStatus);
        Assert.Equal("TRX-900", contract.LastTransactionId);
    }

    [Fact]
    public async Task Reprocessamento_manual_so_e_permitido_para_eventos_com_falha()
    {
        await Factory.SendWebhookAsync(Client, Payload("TRX-950", "CT-5100"));
        var processed = await Factory.WaitForStatusAsync("TRX-950", EventProcessingStatus.Processed);

        var response = await Client.PostAsync($"/api/payments/{processed.Id}/reprocess", content: null);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reprocessamento_manual_reenfileira_um_evento_com_falha()
    {
        var payload = Payload("TRX-960", "CT-5200", 42.00m);
        Guid eventId;

        await using (var context = await Factory.CreateDbContextAsync())
        {
            var eventLog = new WebhookEventLog
            {
                TransactionId = "TRX-960",
                ContractId = "CT-5200",
                Amount = 42.00m,
                PaymentDate = DateTimeOffset.UtcNow.AddHours(-3),
                PaymentStatus = "sucesso",
                Payload = payload,
                PayloadHash = SHA256.HashData(Encoding.UTF8.GetBytes(payload)),
                Status = EventProcessingStatus.PermanentlyFailed,
                ErrorMessage = "Falha simulada",
                Attempts = 5,
                ReceivedAt = DateTimeOffset.UtcNow
            };

            context.WebhookEventLogs.Add(eventLog);
            await context.SaveChangesAsync();
            eventId = eventLog.Id;
        }

        var response = await Client.PostAsync($"/api/payments/{eventId}/reprocess", content: null);

        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);

        var reprocessed = await Factory.WaitForStatusAsync("TRX-960", EventProcessingStatus.Processed);
        Assert.Null(reprocessed.ErrorMessage);

        var contract = await Factory.FindContractAsync("CT-5200");
        Assert.Equal(42.00m, contract!.TotalPaid);
    }
}
