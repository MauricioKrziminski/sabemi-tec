namespace Sabemi.Payments.Core.Security;

public enum SignatureFailureReason
{
    None,
    MissingSignature,
    MalformedSignature,
    MissingTimestamp,
    MalformedTimestamp,
    TimestampOutOfWindow,
    SignatureMismatch
}

public readonly record struct SignatureValidationResult(bool IsValid, SignatureFailureReason Reason)
{
    public static SignatureValidationResult Success { get; } = new(true, SignatureFailureReason.None);

    public static SignatureValidationResult Failure(SignatureFailureReason reason) => new(false, reason);
}
