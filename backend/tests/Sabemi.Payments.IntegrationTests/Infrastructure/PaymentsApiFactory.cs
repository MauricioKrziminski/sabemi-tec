using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sabemi.Payments.Core.Domain;
using Sabemi.Payments.Core.Security;
using Sabemi.Payments.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Sabemi.Payments.IntegrationTests.Infrastructure;

/// <summary>
/// Sobe a API real contra um PostgreSQL descartável.
///
/// O atraso simulado do processamento cai para poucos milissegundos e a varredura de
/// recuperação roda a cada segundo, senão a suíte levaria minutos.
/// </summary>
public sealed class PaymentsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Secret = "segredo-de-teste";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("sabemi_payments_tests")
        .WithUsername("sabemi")
        .WithPassword("sabemi")
        .Build();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // A configuração do teste chega por variável de ambiente porque essa fonte é lida
        // depois do appsettings.json. Passar a connection string por ConfigureAppConfiguration
        // faria o arquivo do projeto vencer, e a suíte acabaria escrevendo no banco de
        // desenvolvimento em vez do container descartável.
        foreach (var (key, value) in Settings)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private Dictionary<string, string> Settings => new()
    {
        ["ConnectionStrings__Payments"] = _database.GetConnectionString(),
        ["Webhook__Secret"] = Secret,
        ["Processing__SimulatedWorkDelay"] = "00:00:00.050",
        ["Processing__InitialRetryDelay"] = "00:00:00.100",
        ["Processing__RecoveryInterval"] = "00:00:01",
        ["Processing__StuckTimeout"] = "00:00:05"
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();

        foreach (var key in Settings.Keys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = await CreateDbContextAsync();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE webhook_event_logs, contract_statuses RESTART IDENTITY CASCADE;");
    }

    public Task<PaymentsDbContext> CreateDbContextAsync() =>
        Services.GetRequiredService<IDbContextFactory<PaymentsDbContext>>().CreateDbContextAsync();

    /// <summary>Assina e envia um webhook exatamente como o banco parceiro faria.</summary>
    public Task<HttpResponseMessage> SendWebhookAsync(
        HttpClient client,
        string body,
        long? timestamp = null,
        string? secret = null,
        string? signature = null)
    {
        var unixSeconds = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/pagamento")
        {
            Content = new StringContent(body, Encoding.UTF8)
        };

        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add("X-Timestamp", unixSeconds.ToString(CultureInfo.InvariantCulture));
        request.Headers.Add(
            "X-Signature",
            signature ?? WebhookSignatureValidator.Compute(secret ?? Secret, unixSeconds, body));

        return client.SendAsync(request);
    }

    public async Task<WebhookEventLog?> FindEventAsync(string transactionId)
    {
        await using var context = await CreateDbContextAsync();
        return await context.WebhookEventLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log => log.TransactionId == transactionId);
    }

    public async Task<ContractStatus?> FindContractAsync(string contractId)
    {
        await using var context = await CreateDbContextAsync();
        return await context.ContractStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(contract => contract.ContractId == contractId);
    }

    /// <summary>
    /// Espera o processamento em background chegar ao estado desejado. A espera é por condição,
    /// e não por tempo fixo, para que o teste não fique lento nem instável.
    /// </summary>
    public async Task<WebhookEventLog> WaitForStatusAsync(
        string transactionId,
        EventProcessingStatus expected,
        TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        WebhookEventLog? current = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            current = await FindEventAsync(transactionId);
            if (current?.Status == expected)
            {
                return current;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException(
            $"O evento {transactionId} não alcançou o estado {expected}. Estado atual: {current?.Status.ToString() ?? "inexistente"}.");
    }
}
