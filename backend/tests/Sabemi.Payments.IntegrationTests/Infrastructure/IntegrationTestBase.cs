using System.Globalization;

namespace Sabemi.Payments.IntegrationTests.Infrastructure;

/// <summary>
/// Um único container PostgreSQL serve toda a suíte. Subir um por teste custaria minutos.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<PaymentsApiFactory>
{
    public const string Name = "integracao";
}

[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTestBase(PaymentsApiFactory factory) : IAsyncLifetime
{
    protected PaymentsApiFactory Factory { get; } = factory;

    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Client = Factory.CreateClient();
        await Factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Monta o payload no contrato acordado com o banco parceiro.</summary>
    protected static string Payload(
        string transactionId,
        string contractId = "CT-1029",
        decimal amount = 100.50m,
        DateTimeOffset? paidAt = null,
        string status = "sucesso")
    {
        var paymentDate = (paidAt ?? DateTimeOffset.UtcNow.AddMinutes(-10)).ToString("O", CultureInfo.InvariantCulture);
        var value = amount.ToString(CultureInfo.InvariantCulture);

        return $$"""
                 {"id_transacao":"{{transactionId}}","id_contrato":"{{contractId}}","valor":{{value}},"data_pagamento":"{{paymentDate}}","status":"{{status}}"}
                 """;
    }
}
