using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Sabemi.Payments.Core.Security;

public sealed class WebhookSignatureValidator(IOptions<WebhookSignatureOptions> options, TimeProvider timeProvider)
{
    private const string SignaturePrefix = "sha256=";

    private readonly WebhookSignatureOptions _options = options.Value;

    public SignatureValidationResult Validate(string rawBody, string? signatureHeader, string? timestampHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return SignatureValidationResult.Failure(SignatureFailureReason.MissingSignature);
        }

        if (string.IsNullOrWhiteSpace(timestampHeader))
        {
            return SignatureValidationResult.Failure(SignatureFailureReason.MissingTimestamp);
        }

        if (!long.TryParse(timestampHeader, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixSeconds))
        {
            return SignatureValidationResult.Failure(SignatureFailureReason.MalformedTimestamp);
        }

        var sentAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        var drift = (timeProvider.GetUtcNow() - sentAt).Duration();
        if (drift > _options.Tolerance)
        {
            return SignatureValidationResult.Failure(SignatureFailureReason.TimestampOutOfWindow);
        }

        if (!TryReadSignature(signatureHeader, out var provided))
        {
            return SignatureValidationResult.Failure(SignatureFailureReason.MalformedSignature);
        }

        var expected = ComputeHash(_options.Secret, unixSeconds, rawBody);

        return CryptographicOperations.FixedTimeEquals(expected, provided)
            ? SignatureValidationResult.Success
            : SignatureValidationResult.Failure(SignatureFailureReason.SignatureMismatch);
    }

    public static string Compute(string secret, long unixSeconds, string rawBody) =>
        SignaturePrefix + Convert.ToHexStringLower(ComputeHash(secret, unixSeconds, rawBody));

    private static byte[] ComputeHash(string secret, long unixSeconds, string rawBody)
    {
        var message = Encoding.UTF8.GetBytes(
            string.Create(CultureInfo.InvariantCulture, $"{unixSeconds}.{rawBody}"));

        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), message);
    }

    private static bool TryReadSignature(string header, out byte[] signature)
    {
        var value = header.Trim();
        if (value.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
        {
            value = value[SignaturePrefix.Length..];
        }

        try
        {
            signature = Convert.FromHexString(value);
            return signature.Length == SHA256.HashSizeInBytes;
        }
        catch (FormatException)
        {
            signature = [];
            return false;
        }
    }
}
