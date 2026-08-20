namespace Sabemi.Payments.Core.Domain;

/// <summary>
/// Situação consolidada de um contrato, derivada dos eventos já processados.
/// </summary>
public sealed class ContractStatus
{
    /// <summary>Chave natural, evita um join no caminho quente do processamento.</summary>
    public required string ContractId { get; init; }

    /// <summary>Resultado do pagamento mais recente do contrato, em português, como no payload.</summary>
    public required string LastStatus { get; set; }

    public required string LastTransactionId { get; set; }

    public DateTimeOffset LastPaymentDate { get; set; }

    /// <summary>Soma dos pagamentos liquidados com sucesso.</summary>
    public decimal TotalPaid { get; set; }

    /// <summary>Quantidade de pagamentos liquidados com sucesso.</summary>
    public int PaymentCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
