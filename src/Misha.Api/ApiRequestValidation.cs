namespace Misha.Api;

internal static class ApiRequestValidation
{
    internal const int MaxCurrencyLength = 3;
    internal const int MaxEtaNumberLength = 100;
    internal const int MaxVerificationTokenLength = 4096;
    internal const int MaxReasonLength = 1000;
    internal const int MaxRecipientReferenceLength = 200;
    internal const int MaxChannelLength = 50;
    internal const int MaxTemplateLength = 200;
    internal const int MaxNotificationPayloadLength = 64 * 1024;
    internal const int MaxManualReviewPageSize = 100;
    internal const int DefaultManualReviewPageSize = 50;
    internal const int MaxNotificationPageSize = 100;
    internal const int DefaultNotificationPageSize = 50;

    internal static Dictionary<string, string[]>? ValidateCreatePayment(CreatePaymentRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (request.AmountMinor <= 0)
            errors[nameof(request.AmountMinor)] = ["AmountMinor must be greater than zero."];
        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != MaxCurrencyLength || request.Currency.Any(c => !char.IsLetter(c)))
            errors[nameof(request.Currency)] = ["Currency must be a three-letter code."];
        return errors.Count == 0 ? null : errors;
    }

    internal static Dictionary<string, string[]>? ValidateEtaVerification(EtaVerificationRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        AddRequiredBounded(errors, nameof(request.EtaNumber), request.EtaNumber, MaxEtaNumberLength);
        AddRequiredBounded(errors, nameof(request.VerificationToken), request.VerificationToken, MaxVerificationTokenLength);
        return errors.Count == 0 ? null : errors;
    }

    internal static Dictionary<string, string[]>? ValidateEtaRevocation(EtaRevocationRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        AddRequiredBounded(errors, nameof(request.Reason), request.Reason, MaxReasonLength);
        return errors.Count == 0 ? null : errors;
    }

    internal static Dictionary<string, string[]>? ValidateNotification(QueueNotificationRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        AddRequiredBounded(errors, nameof(request.RecipientReference), request.RecipientReference, MaxRecipientReferenceLength);
        AddRequiredBounded(errors, nameof(request.Channel), request.Channel, MaxChannelLength);
        AddRequiredBounded(errors, nameof(request.Template), request.Template, MaxTemplateLength);
        AddRequiredBounded(errors, nameof(request.Payload), request.Payload, MaxNotificationPayloadLength);
        return errors.Count == 0 ? null : errors;
    }

    internal static Dictionary<string, string[]>? ValidateManualReviewResolution(ManualReviewEndpoints.ResolveManualReviewRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (!Enum.IsDefined(request.Resolution))
            errors[nameof(request.Resolution)] = ["Resolution is invalid."];
        AddRequiredBounded(errors, nameof(request.Reason), request.Reason, MaxReasonLength);
        return errors.Count == 0 ? null : errors;
    }

    internal static int NormalizePageSize(int? requested, int defaultValue, int maxValue)
    {
        if (requested is null)
            return defaultValue;
        return Math.Clamp(requested.Value, 1, maxValue);
    }

    private static void AddRequiredBounded(Dictionary<string, string[]> errors, string name, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[name] = ["Value is required."];
            return;
        }

        if (value.Length > maxLength)
            errors[name] = [$"Value must not exceed {maxLength} characters."];
    }
}
