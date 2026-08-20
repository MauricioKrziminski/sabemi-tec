namespace Sabemi.Payments.Core.Security;

public sealed class WebhookSignatureOptions
{
    public const string SectionName = "Webhook";

    public string Secret { get; set; } = string.Empty;

    public TimeSpan Tolerance { get; set; } = TimeSpan.FromMinutes(5);

    public string SignatureHeader { get; set; } = "X-Signature";

    public string TimestampHeader { get; set; } = "X-Timestamp";
}
