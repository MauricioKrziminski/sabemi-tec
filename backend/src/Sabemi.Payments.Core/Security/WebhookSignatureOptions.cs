namespace Sabemi.Payments.Core.Security;

public sealed class WebhookSignatureOptions
{
    public const string SectionName = "Webhook";

    /// <summary>Segredo compartilhado com o banco parceiro.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Janela aceita entre o carimbo de tempo da requisição e o relógio do servidor.</summary>
    public TimeSpan Tolerance { get; set; } = TimeSpan.FromMinutes(5);

    public string SignatureHeader { get; set; } = "X-Signature";

    public string TimestampHeader { get; set; } = "X-Timestamp";
}
